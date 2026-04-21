using CP.Migrator.Business.Config;
using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;
using System.Globalization;

namespace CP.Migrator.Business.Validation
{
	/// <summary>
	/// Validates a <see cref="TreatmentCsvRow"/> against Core Practice schema rules.
	/// Implements <see cref="ICrossRecordValidator{TRow,TContextRow}"/> because treatment
	/// validation requires cross-referencing against the loaded patient data — a treatment
	/// must reference a patient that exists in the same import session.
	/// <para>
	/// <strong>Note:</strong> This class caches the patient-ID lookup between calls for
	/// performance. It is safe to reuse within a single-threaded import session but is
	/// <em>not</em> thread-safe.
	/// </para>
	/// Rules (paid values, date format) are driven by <see cref="TreatmentValidationOptions"/>
	/// so they can be changed without code edits.
	/// </summary>
	internal class TreatmentValidator : ICrossRecordValidator<TreatmentCsvRow, PatientCsvRow>
	{
		private readonly TreatmentValidationOptions _options;

		// Cache the patient-ID lookup so repeated Validate() calls with the same
		// patient context reference don't rebuild the HashSet every time.
		private IEnumerable<ParsedRow<PatientCsvRow>>? _lastPatientContextRef;
		private HashSet<string> _knownPatientSourceIds = new(StringComparer.OrdinalIgnoreCase);

		// Cache the duplicate-ID lookup so repeated Validate() calls with the same
		// treatment rows reference don't rebuild the lookup every time.
		private IEnumerable<ParsedRow<TreatmentCsvRow>>? _lastTreatmentRowsRef;
		private Dictionary<string, int> _sourceIdToFirstRow = new(StringComparer.OrdinalIgnoreCase);

		public TreatmentValidator() : this(new TreatmentValidationOptions()) { }

		public TreatmentValidator(TreatmentValidationOptions options)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public void Validate(
			ParsedRow<TreatmentCsvRow> parsedRow,
			IEnumerable<ParsedRow<TreatmentCsvRow>> allRows,
			IEnumerable<ParsedRow<PatientCsvRow>> contextRows)
		{
			var row = parsedRow.Row;

			// Rebuild the patient-ID lookup only when the patient context reference changes
			if (!ReferenceEquals(contextRows, _lastPatientContextRef))
			{
				_lastPatientContextRef = contextRows;
				_knownPatientSourceIds = new HashSet<string>(
					contextRows
						.Select(p => p.Row.RawSourceId)
						.Where(id => !string.IsNullOrWhiteSpace(id))!,
					StringComparer.OrdinalIgnoreCase);
			}

			// Rebuild the duplicate lookups only when the treatment rows reference changes
			if (!ReferenceEquals(allRows, _lastTreatmentRowsRef))
			{
				_lastTreatmentRowsRef = allRows;
				_sourceIdToFirstRow = allRows
					.Where(r => !string.IsNullOrWhiteSpace(r.Row.RawSourceId))
					.GroupBy(r => r.Row.RawSourceId, StringComparer.OrdinalIgnoreCase)
					.ToDictionary(g => g.Key, g => g.Min(r => r.Row.RowIndex), StringComparer.OrdinalIgnoreCase);
			}

			// --- Intra-CSV duplicate treatment ID (considered Error) ---
			if (!string.IsNullOrWhiteSpace(row.RawSourceId)
				&& _sourceIdToFirstRow.TryGetValue(row.RawSourceId, out var firstIdRow)
				&& firstIdRow != row.RowIndex)
			{
				parsedRow.Errors.Add(new ValidationError("ID",
					$"Duplicate treatment ID '{row.RawSourceId}' — already seen on row {firstIdRow}. Remove this row or change the ID."));
			}

			// --- Patient cross-reference ---
			if (string.IsNullOrWhiteSpace(row.RawPatientSourceId))
			{
				parsedRow.Errors.Add(new ValidationError("PatientID", "Patient ID is required."));
			}
			else if (!_knownPatientSourceIds.Contains(row.RawPatientSourceId))
			{
				parsedRow.Errors.Add(new ValidationError("PatientID",
					$"Patient ID '{row.RawPatientSourceId}' does not match any patient in the loaded patient file."));
			}

			// --- TreatmentItem (required, max 10) ---
			if (string.IsNullOrWhiteSpace(row.TreatmentItem))
				parsedRow.Errors.Add(new ValidationError("TreatmentItem", "Treatment item code is required."));
			else if (row.TreatmentItem.Length > 10)
				parsedRow.Errors.Add(new ValidationError("TreatmentItem",
					$"Treatment item code exceeds maximum length of 10 characters ({row.TreatmentItem.Length} provided)."));

			// --- Description (required, max 512) ---
			if (string.IsNullOrWhiteSpace(row.Description))
				parsedRow.Errors.Add(new ValidationError("Description", "Description is required."));
			else if (row.Description.Length > 512)
				parsedRow.Errors.Add(new ValidationError("Description",
					$"Description exceeds maximum length of 512 characters ({row.Description.Length} provided)."));

			// --- Fee (required) ---
			if (string.IsNullOrWhiteSpace(row.Fee))
			{
				parsedRow.Errors.Add(new ValidationError("Fee", "Fee is required."));
			}
			else if (!decimal.TryParse(row.Fee, NumberStyles.Any, CultureInfo.InvariantCulture, out var fee)
					 || fee <= 0)
			{
				parsedRow.Errors.Add(new ValidationError("Fee",
					$"'{row.Fee}' is not a valid positive fee amount."));
			}

			// --- Treatment date (optional) ---
			if (!string.IsNullOrWhiteSpace(row.Date)
				&& !DateTime.TryParseExact(row.Date, _options.ExpectedDateFormat, CultureInfo.InvariantCulture,
					 DateTimeStyles.None, out _))
			{
				parsedRow.Errors.Add(new ValidationError("Date",
					$"'{row.Date}' is not a recognised date. Expected {_options.ExpectedDateFormat}. Try auto-fix."));
			}

			// --- ToothNumber (optional, max 10) ---
			if (!string.IsNullOrWhiteSpace(row.ToothNumber) && row.ToothNumber.Length > 10)
				parsedRow.Errors.Add(new ValidationError("ToothNumber",
					$"Tooth number exceeds maximum length of 10 characters ({row.ToothNumber.Length} provided)."));

			// --- Surface (optional, max 20) ---
			if (!string.IsNullOrWhiteSpace(row.Surface) && row.Surface.Length > 20)
				parsedRow.Errors.Add(new ValidationError("Surface",
					$"Surface exceeds maximum length of 20 characters ({row.Surface.Length} provided)."));

			// --- Paid flag (required) ---
			if (string.IsNullOrWhiteSpace(row.Paid))
			{
				parsedRow.Errors.Add(new ValidationError("Paid",
					"Paid status is required (Yes/No).", ValidationSeverity.Warning));
			}
			else if (!_options.ValidPaidValues.Contains(row.Paid))
			{
				var accepted = string.Join("/", _options.ValidPaidValues);
				parsedRow.Errors.Add(new ValidationError("Paid",
					$"'{row.Paid}' is not valid. Must be '{accepted}'. Try auto-fix."));
			}
		}
	}
}

