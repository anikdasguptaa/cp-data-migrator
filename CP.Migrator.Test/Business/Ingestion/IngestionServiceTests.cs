using CP.Migrator.Business.Ingestion;
using CP.Migrator.Data.SQLite;
using CP.Migrator.Data.SQLite.Repositories;
using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;
using CP.Migrator.Test.Shared;
using System.Data;

namespace CP.Migrator.Test.Business.Ingestion;

/// <summary>
/// Integration tests for <see cref="IngestionService"/> using a real in-memory SQLite database.
/// Each test gets an isolated database via <see cref="FixedConnectionFactory"/>.
/// </summary>
public class IngestionServiceTests : IDisposable
{
    private readonly FixedConnectionFactory _factory = new();
    private readonly IDbConnection _keepAlive;
    private readonly SQLiteUnitOfWork _uow;
    private readonly IngestionService _sut;

    public IngestionServiceTests()
    {
        _keepAlive = _factory.CreateConnection();
        _keepAlive.Open();
        new SQLiteDatabaseInitializer(_factory).Initialise();

        _uow = new SQLiteUnitOfWork(_factory);
        _sut = new IngestionService(
            _uow,
            new SQLitePatientRepository(_uow),
            new SQLiteInvoiceRepository(_uow),
            new SQLiteTreatmentRepository(_uow),
            new SQLiteInvoiceLineItemRepository(_uow));
    }

    public void Dispose()
    {
        _uow.Dispose();
        _keepAlive.Dispose();
    }

    private const int TestClinicId = 1;

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static ParsedRow<PatientCsvRow> ValidPatient(string id = "P1") =>
        new(new PatientCsvRow
        {
            RawSourceId = id,
            FirstName = "Jane",
            LastName = "Smith",
            DOB = "1990-06-15",
            Gender = "F",
            State = "NSW",
            Postcode = "2000",
            RowIndex = 1
        });

    private static ParsedRow<TreatmentCsvRow> ValidTreatment(string id, string patientId, string date = "2024-03-15") =>
        new(new TreatmentCsvRow
        {
            RawSourceId = id,
            RawPatientSourceId = patientId,
            TreatmentItem = "011",
            Description = "Exam",
            Fee = "150.00",
            Date = date,
            Paid = "Yes",
            RowIndex = 1
        });

    private static ParsedRow<PatientCsvRow> InvalidPatient(string id = "BAD")
    {
        var row = new PatientCsvRow { RawSourceId = id, RowIndex = 2 };
        var parsedRow = new ParsedRow<PatientCsvRow>(row);
        parsedRow.Errors.Add(new ValidationError("FirstName", "First name is required."));
        return parsedRow;
    }

    private static ParsedRow<TreatmentCsvRow> InvalidTreatment(string id = "TBAD")
    {
        var row = new TreatmentCsvRow { RawSourceId = id, RowIndex = 2 };
        var parsedRow = new ParsedRow<TreatmentCsvRow>(row);
        parsedRow.Errors.Add(new ValidationError("Fee", "Fee is required."));
        return parsedRow;
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task IngestAsync_ValidPatientAndTreatment_InsertsAll()
    {
        var patient = ValidPatient("P1");
        var treatment = ValidTreatment("T1", "P1");

        var report = await _sut.IngestAsync(TestClinicId, [patient], [treatment]);

        Assert.Equal(2, report.TotalRows);
        Assert.Equal(2, report.SuccessCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Equal(0, report.ErrorCount);
    }

    [Fact]
    public async Task IngestAsync_InvalidPatient_SkipsPatient()
    {
        var report = await _sut.IngestAsync(TestClinicId, [InvalidPatient()], []);

        Assert.Equal(1, report.SkippedCount);
        Assert.Equal(0, report.SuccessCount);
    }

    [Fact]
    public async Task IngestAsync_InvalidTreatment_SkipsTreatment()
    {
        var report = await _sut.IngestAsync(TestClinicId, [ValidPatient("P1")], [InvalidTreatment()]);

        // patient inserted, treatment skipped
        Assert.Equal(1, report.SuccessCount);
        Assert.Equal(1, report.SkippedCount);
    }

    [Fact]
    public async Task IngestAsync_TreatmentWithOrphanedPatientId_SkipsTreatment()
    {
        // Treatment references a patient that was not successfully ingested
        var patient = ValidPatient("P1");
        var treatment = ValidTreatment("T1", "MISSING");

        var report = await _sut.IngestAsync(TestClinicId, [patient], [treatment]);

        Assert.Equal(1, report.SuccessCount); // patient inserted
        Assert.Equal(1, report.SkippedCount); // treatment skipped (patient not in map)
    }

    [Fact]
    public async Task IngestAsync_MultipleTreatmentsSamePatientAndDate_GroupedIntoOneInvoice()
    {
        var patient = ValidPatient("P1");
        var t1 = ValidTreatment("T1", "P1", "2024-03-15");
        var t2 = ValidTreatment("T2", "P1", "2024-03-15");

        var report = await _sut.IngestAsync(TestClinicId, [patient], [t1, t2]);

        // 1 patient + 2 treatments = 3 inserted entries
        Assert.Equal(3, report.SuccessCount);
    }

    [Fact]
    public async Task IngestAsync_TreatmentsDifferentDates_CreatesOneInvoicePerDate()
    {
        var patient = ValidPatient("P1");
        var t1 = ValidTreatment("T1", "P1", "2024-03-15");
        var t2 = ValidTreatment("T2", "P1", "2024-04-10");

        var report = await _sut.IngestAsync(TestClinicId, [patient], [t1, t2]);

        // 1 patient + 2 treatments = 3 success entries
        Assert.Equal(3, report.SuccessCount);
    }

    [Fact]
    public async Task IngestAsync_DuplicatePatient_ReportsDuplicate()
    {
        var patient = ValidPatient("P1");

        // First ingestion
        await _sut.IngestAsync(TestClinicId, [patient], []);

        // Second ingestion of the same patient
        var report = await _sut.IngestAsync(TestClinicId, [patient], []);

        Assert.Equal(1, report.DuplicateCount);
        Assert.Equal(0, report.SuccessCount);
    }

    [Fact]
    public async Task IngestAsync_DuplicatePatient_StillIngestsNewTreatments()
    {
        var patient = ValidPatient("P1");

        // First ingestion — patient inserted
        await _sut.IngestAsync(TestClinicId, [patient], []);

        // Second ingestion — same patient (duplicate) but with new treatments
        var treatment = ValidTreatment("T1", "P1");
        var report = await _sut.IngestAsync(TestClinicId, [patient], [treatment]);

        Assert.Equal(1, report.DuplicateCount);  // patient is a duplicate
        Assert.Equal(1, report.SuccessCount);     // treatment still ingested against existing patient
    }

    [Fact]
    public async Task IngestAsync_ReingestionOfSameTreatment_InsertsAgain()
    {
        var patient = ValidPatient("P1");
        var treatment = ValidTreatment("T1", "P1");

        // First ingestion — everything inserted
        var first = await _sut.IngestAsync(TestClinicId, [patient], [treatment]);
        Assert.Equal(2, first.SuccessCount);

        // Second ingestion — same patient (duplicate) but treatment is re-inserted
        var report = await _sut.IngestAsync(TestClinicId, [patient], [treatment]);

        Assert.Equal(1, report.DuplicateCount); // patient is a duplicate
        Assert.Equal(1, report.SuccessCount);   // treatment inserted again (no duplicate check for treatments)
    }

    [Fact]
    public async Task IngestAsync_EmptyInputs_ReturnsEmptyReport()
    {
        var report = await _sut.IngestAsync(TestClinicId, [], []);

        Assert.Equal(0, report.TotalRows);
        Assert.Equal(0, report.SuccessCount);
        Assert.Empty(report.Entries);
    }

    [Fact]
    public async Task IngestAsync_MixedValidAndInvalidPatients_ProcessesValidOnly()
    {
        var valid = ValidPatient("P1");
        var invalid = InvalidPatient("P2");

        var report = await _sut.IngestAsync(TestClinicId, [valid, invalid], []);

        Assert.Equal(1, report.SuccessCount);
        Assert.Equal(1, report.SkippedCount);
    }
}
