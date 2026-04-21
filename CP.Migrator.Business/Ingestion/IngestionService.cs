using CP.Migrator.Data;
using CP.Migrator.Data.Repositories;
using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Entities;
using CP.Migrator.Models.Results;
using System.Globalization;

namespace CP.Migrator.Business.Ingestion
{
    /// <summary>
    /// Orchestrates the full ingestion pipeline for a single import session.
    /// See <see cref="IIngestionService"/> for the contract and sequencing rules.
    /// </summary>
    internal class IngestionService : IIngestionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPatientRepository _patientRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ITreatmentRepository _treatmentRepository;
        private readonly IInvoiceLineItemRepository _lineItemRepository;

        public IngestionService(
            IUnitOfWork unitOfWork,
            IPatientRepository patientRepository,
            IInvoiceRepository invoiceRepository,
            ITreatmentRepository treatmentRepository,
            IInvoiceLineItemRepository lineItemRepository)
        {
            _unitOfWork = unitOfWork;
            _patientRepository = patientRepository;
            _invoiceRepository = invoiceRepository;
            _treatmentRepository = treatmentRepository;
            _lineItemRepository = lineItemRepository;
        }

        public async Task<IngestionReport> IngestAsync(
            int clinicId,
            IEnumerable<ParsedRow<PatientCsvRow>> patientRows,
            IEnumerable<ParsedRow<TreatmentCsvRow>> treatmentRows)
        {
            var report = new IngestionReport();

            var patients = patientRows.ToList();
            var treatments = treatmentRows.ToList();

            report.TotalRows = patients.Count + treatments.Count;

            _unitOfWork.BeginTransaction();

            try
            {
                // ---------------------------------------------------------------
                // Step 1: Insert valid patients — build RawSourceId → DB PatientId
                // ---------------------------------------------------------------
                var patientIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var parsedRow in patients)
                {
                    if (!parsedRow.IsValid)
                    {
                        report.Entries.Add(Skipped(parsedRow.Row, parsedRow.Errors));
                        continue;
                    }

                    try
                    {
                        var entity = MapPatient(parsedRow.Row, clinicId);

                        // Check for duplicate patient before inserting
                        var existingId = await _patientRepository.FindByCompositeKeyAsync(
                            clinicId, entity.PatientNo, entity.Firstname, entity.Lastname);

                        if (existingId.HasValue)
                        {
                            // Map the CSV source ID to the existing DB patient so that
                            // treatments referencing this patient are still ingested
                            // against the already-persisted record.
                            patientIdMap[parsedRow.Row.RawSourceId] = existingId.Value;

                            report.Entries.Add(Duplicate(parsedRow.Row,
                                $"Patient already exists (DB ID {existingId.Value}). " +
                                $"Matched on ClinicId '{clinicId}', PatientNo '{entity.PatientNo}', Name '{entity.Firstname} {entity.Lastname}'. " +
                                $"Treatments will be linked to the existing patient."));
                            continue;
                        }

                        var dbId = await _patientRepository.InsertAsync(entity);
                        patientIdMap[parsedRow.Row.RawSourceId] = dbId;
                        report.Entries.Add(Inserted(parsedRow.Row, $"Patient inserted (DB ID {dbId})."));
                    }
                    catch (Exception ex)
                    {
                        report.Entries.Add(Failed(parsedRow.Row, ex.Message));
                    }
                }

                // ---------------------------------------------------------------
                // Step 2: Record skipped treatment rows up front
                // ---------------------------------------------------------------
                foreach (var parsedRow in treatments.Where(t => !t.IsValid))
                    report.Entries.Add(Skipped(parsedRow.Row, parsedRow.Errors));

                // Commit and return if there are no valid treatments
                var validTreatments = treatments.Where(t => t.IsValid).ToList();
                if (validTreatments.Count == 0)
                {
                    _unitOfWork.Commit();
                    return report;
                }

				// ---------------------------------------------------------------
				// Step 3: Group valid treatments by (PatientSourceId, Date)
				//         → one Invoice per group
				// ---------------------------------------------------------------
				var invoiceGroups = validTreatments
                    .GroupBy(t => (t.Row.RawPatientSourceId, t.Row.Date))
                    .ToList();

                int invoiceNumber = await _invoiceRepository.GetMaxInvoiceNoAsync(clinicId);

                foreach (var group in invoiceGroups)
                {
                    var (rawPatientId, treatmentDate) = group.Key;
                    var groupList = group.ToList();

                    // Patient must have been successfully inserted in this session
                    if (!patientIdMap.TryGetValue(rawPatientId, out var dbPatientId))
                    {
                        foreach (var t in groupList)
                            report.Entries.Add(Skipped(t.Row,
                                $"Patient ID '{rawPatientId}' was not successfully ingested - treatment skipped."));
                        continue;
                    }

                    try
                    {
                        var totalFee = groupList.Sum(t => ParseDecimal(t.Row.Fee));
                        var paidAmount = groupList
                            .Where(t => "Yes".Equals(t.Row.Paid, StringComparison.OrdinalIgnoreCase))
                            .Sum(t => ParseDecimal(t.Row.Fee));

                        // --- Invoice ---
                        var invoice = new Invoice
                        {
                            InvoiceIdentifier = Guid.NewGuid().ToString(),
                            InvoiceNo = ++invoiceNumber,
                            InvoiceDate = NullIfEmpty(treatmentDate),
                            DueDate = string.IsNullOrWhiteSpace(treatmentDate) ? null : TryAddDays(treatmentDate, 14),
                            Note = null,
                            Total = totalFee,
                            Paid = paidAmount > 0 ? paidAmount : null,
                            Discount = null,
                            PatientId = dbPatientId,
                            ClinicId = clinicId,
                            IsDeleted = 0
                        };

                        var invoiceId = await _invoiceRepository.InsertAsync(invoice);

                        // --- Insert treatments and line items ---
                        foreach (var parsedTreatment in groupList)
                        {
                            var fee = ParseDecimal(parsedTreatment.Row.Fee);
                            var isPaid = "Yes".Equals(parsedTreatment.Row.Paid, StringComparison.OrdinalIgnoreCase) ? 1 : 0;

                            var treatment = new Treatment
                            {
                                TreatmentIdentifier = Guid.NewGuid().ToString(),
                                CompleteDate = NullIfEmpty(parsedTreatment.Row.Date),
                                Description = parsedTreatment.Row.Description,
                                ItemCode = parsedTreatment.Row.TreatmentItem,
                                Tooth = NullIfEmpty(parsedTreatment.Row.ToothNumber),
                                Surface = NullIfEmpty(parsedTreatment.Row.Surface),
                                Quantity = 1,
                                Fee = fee,
                                InvoiceId = invoiceId,
                                PatientId = dbPatientId,
                                ClinicId = clinicId,
                                IsPaid = isPaid,
                                IsVoided = 0
                            };

                            var treatmentId = await _treatmentRepository.InsertAsync(treatment);

                            var lineItem = new InvoiceLineItem
                            {
                                InvoiceLineItemIdentifier = Guid.NewGuid().ToString(),
                                Description = parsedTreatment.Row.Description,
                                ItemCode = parsedTreatment.Row.TreatmentItem,
                                Quantity = 1,
                                UnitAmount = fee,
                                LineAmount = fee,
                                PatientId = dbPatientId,
                                TreatmentId = treatmentId,
                                InvoiceId = invoiceId,
                                ClinicId = clinicId
                            };

                            await _lineItemRepository.InsertAsync(lineItem);

                            report.Entries.Add(Inserted(parsedTreatment.Row,
                                $"Treatment inserted (DB ID {treatmentId}) on Invoice {invoiceId}."));
                        }
                    }
                    catch (Exception ex)
                    {
                        foreach (var t in groupList)
                            report.Entries.Add(Failed(t.Row, ex.Message));
                    }
                }

                _unitOfWork.Commit();
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }

            return report;
        }

        // -----------------------------------------------------------------------
        // Mapping helpers
        // -----------------------------------------------------------------------

        private static Patient MapPatient(PatientCsvRow row, int clinicId) => new()
        {
            PatientIdentifier = Guid.NewGuid().ToString(),
            PatientNo = row.RawSourceId,           // preserve original CSV ID as a reference number
            Firstname = row.FirstName,
            Lastname = row.LastName,
            DateOfBirth = row.DOB,
            Sex = row.Gender,
            Email = row.Email,
            Mobile = row.MobileNumber,
            HomePhone = row.PhoneNumber,
            AddressLine1 = row.Street,
            Suburb = row.Suburb,
            State = row.State,
            Postcode = row.Postcode,
            Country = "Australia",
            ClinicId = clinicId,
            IsDeleted = 0
        };

        private static decimal ParseDecimal(string? value) =>
            decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;

        /// <summary>Adds days to a yyyy-MM-dd string; returns the original string on failure.</summary>
        private static string TryAddDays(string date, int days)
        {
            if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsed))
                return parsed.AddDays(days).ToString("yyyy-MM-dd");

            return date;
        }

        // -----------------------------------------------------------------------
        // Report entry factories — keep report construction consistent
        // -----------------------------------------------------------------------

        private static IngestionReportEntry Inserted(CsvRow row, string message) => new()
        {
            RowIndex = row.RowIndex,
            RawSourceId = row.RawSourceId,
            Status = IngestionStatus.Inserted,
            Message = message
        };

        private static IngestionReportEntry Skipped(CsvRow row, IEnumerable<ValidationError> errors) => new()
        {
            RowIndex = row.RowIndex,
            RawSourceId = row.RawSourceId,
            Status = IngestionStatus.Skipped,
            Message = $"Skipped - {string.Join("; ", errors.Select(e => e.ToString()))}"
        };

        private static IngestionReportEntry Skipped(CsvRow row, string reason) => new()
        {
            RowIndex = row.RowIndex,
            RawSourceId = row.RawSourceId,
            Status = IngestionStatus.Skipped,
            Message = $"Skipped - {reason}"
        };

        private static IngestionReportEntry Failed(CsvRow row, string message) => new()
        {
            RowIndex = row.RowIndex,
            RawSourceId = row.RawSourceId,
            Status = IngestionStatus.Failed,
            Message = $"Failed - {message}"
        };

        private static IngestionReportEntry Duplicate(CsvRow row, string message) => new()
        {
            RowIndex = row.RowIndex,
            RawSourceId = row.RawSourceId,
            Status = IngestionStatus.Duplicate,
            Message = $"Duplicate - {message}"
        };
    }
}
