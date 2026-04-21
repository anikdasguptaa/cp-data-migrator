using CP.Migrator.Business.Validation;
using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;

namespace CP.Migrator.Test.Business.Validation;

public class TreatmentValidatorTests
{
    private static PatientCsvRow Patient(string id) => new()
    {
        RawSourceId = id,
        FirstName = "John",
        LastName = "Doe",
        DOB = "1980-01-01"
    };

    private static TreatmentCsvRow ValidTreatment(string patientId = "P1") => new()
    {
        RawSourceId = "T1",
        RawPatientSourceId = patientId,
        TreatmentItem = "011",
        Description = "Exam",
        Fee = "150.00",
        Date = "2024-03-15",
        Paid = "Yes"
    };

    private static List<ParsedRow<PatientCsvRow>> PatientContext(params string[] ids) =>
        ids.Select(id => new ParsedRow<PatientCsvRow>(Patient(id))).ToList();

    private readonly TreatmentValidator _sut = new();

    [Fact]
    public void Validate_ValidRow_NoErrors()
    {
        var parsedRow = new ParsedRow<TreatmentCsvRow>(ValidTreatment());
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.Empty(parsedRow.Errors.Where(e => e.Severity == ValidationSeverity.Error));
    }

    [Fact]
    public void Validate_MissingPatientId_AddsError()
    {
        var row = ValidTreatment();
        row.RawPatientSourceId = "";
        var parsedRow = new ParsedRow<TreatmentCsvRow>(row);
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "PatientID");
    }

    [Fact]
    public void Validate_UnknownPatientId_AddsError()
    {
        var parsedRow = new ParsedRow<TreatmentCsvRow>(ValidTreatment("UNKNOWN"));
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "PatientID" && e.Message.Contains("UNKNOWN"));
    }

    [Fact]
    public void Validate_MissingTreatmentItem_AddsError()
    {
        var row = ValidTreatment();
        row.TreatmentItem = null!;
        var parsedRow = new ParsedRow<TreatmentCsvRow>(row);
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "TreatmentItem");
    }

    [Fact]
    public void Validate_MissingDescription_AddsError()
    {
        var row = ValidTreatment();
        row.Description = "";
        var parsedRow = new ParsedRow<TreatmentCsvRow>(row);
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "Description");
    }

    [Fact]
    public void Validate_MissingFee_AddsError()
    {
        var row = ValidTreatment();
        row.Fee = null!;
        var parsedRow = new ParsedRow<TreatmentCsvRow>(row);
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "Fee");
    }

    [Fact]
    public void Validate_ZeroFee_AddsError()
    {
        var row = ValidTreatment();
        row.Fee = "0";
        var parsedRow = new ParsedRow<TreatmentCsvRow>(row);
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "Fee");
    }

    [Fact]
    public void Validate_NegativeFee_AddsError()
    {
        var row = ValidTreatment();
        row.Fee = "-10.00";
        var parsedRow = new ParsedRow<TreatmentCsvRow>(row);
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "Fee");
    }

    [Fact]
    public void Validate_NonNumericFee_AddsError()
    {
        var row = ValidTreatment();
        row.Fee = "abc";
        var parsedRow = new ParsedRow<TreatmentCsvRow>(row);
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "Fee");
    }

    [Fact]
    public void Validate_MissingDate_NoError()
    {
        var row = ValidTreatment();
        row.Date = "";
        var parsedRow = new ParsedRow<TreatmentCsvRow>(row);
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.DoesNotContain(parsedRow.Errors, e => e.FieldName == "Date");
    }

    [Fact]
    public void Validate_InvalidDateFormat_AddsError()
    {
        var row = ValidTreatment();
        row.Date = "15/03/2024";
        var parsedRow = new ParsedRow<TreatmentCsvRow>(row);
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "Date");
    }

    [Fact]
    public void Validate_MissingPaid_AddsWarning()
    {
        var row = ValidTreatment();
        row.Paid = "";
        var parsedRow = new ParsedRow<TreatmentCsvRow>(row);
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "Paid" && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void Validate_InvalidPaid_AddsError()
    {
        var row = ValidTreatment();
        row.Paid = "Maybe";
        var parsedRow = new ParsedRow<TreatmentCsvRow>(row);
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "Paid" && e.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void Validate_PatientIdLookup_IsCaseInsensitive()
    {
        var parsedRow = new ParsedRow<TreatmentCsvRow>(ValidTreatment("p1"));
        _sut.Validate(parsedRow, [parsedRow], PatientContext("P1"));
        Assert.DoesNotContain(parsedRow.Errors, e => e.FieldName == "PatientID");
    }

    [Fact]
    public void Validate_ContextCacheIsReused_WhenSameReference()
    {
        var context = PatientContext("P1");
        var parsedRow1 = new ParsedRow<TreatmentCsvRow>(ValidTreatment("P1"));
        var parsedRow2 = new ParsedRow<TreatmentCsvRow>(ValidTreatment("P1"));

        _sut.Validate(parsedRow1, [parsedRow1], context);
        _sut.Validate(parsedRow2, [parsedRow2], context);

        Assert.Empty(parsedRow1.Errors.Where(e => e.FieldName == "PatientID"));
        Assert.Empty(parsedRow2.Errors.Where(e => e.FieldName == "PatientID"));
    }

    [Fact]
    public void Validate_DuplicateTreatmentSourceId_AddsError()
    {
        var row1 = ValidTreatment();
        row1.RawSourceId = "T1";
        row1.RowIndex = 1;

        var row2 = ValidTreatment();
        row2.RawSourceId = "T1";
        row2.RowIndex = 2;

        var parsedRow1 = new ParsedRow<TreatmentCsvRow>(row1);
        var parsedRow2 = new ParsedRow<TreatmentCsvRow>(row2);
        var allRows = new List<ParsedRow<TreatmentCsvRow>> { parsedRow1, parsedRow2 };
        var context = PatientContext("P1");

        _sut.Validate(parsedRow1, allRows, context);
        _sut.Validate(parsedRow2, allRows, context);

        Assert.DoesNotContain(parsedRow1.Errors, e => e.FieldName == "ID");
        Assert.Contains(parsedRow2.Errors, e => e.FieldName == "ID" && e.Message.Contains("row 1"));
    }

    [Fact]
    public void Validate_UniqueTreatmentSourceIds_NoDuplicateError()
    {
        var row1 = ValidTreatment();
        row1.RawSourceId = "T1";
        row1.RowIndex = 1;

        var row2 = ValidTreatment();
        row2.RawSourceId = "T2";
        row2.RowIndex = 2;

        var parsedRow1 = new ParsedRow<TreatmentCsvRow>(row1);
        var parsedRow2 = new ParsedRow<TreatmentCsvRow>(row2);
        var allRows = new List<ParsedRow<TreatmentCsvRow>> { parsedRow1, parsedRow2 };
        var context = PatientContext("P1");

        _sut.Validate(parsedRow1, allRows, context);
        _sut.Validate(parsedRow2, allRows, context);

        Assert.DoesNotContain(parsedRow1.Errors, e => e.FieldName == "ID");
        Assert.DoesNotContain(parsedRow2.Errors, e => e.FieldName == "ID");
    }
}
