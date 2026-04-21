using CP.Migrator.Business.Config;
using CP.Migrator.Business.Validation;
using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;

namespace CP.Migrator.Test.Business.Validation;

public class PatientValidatorTests
{
    private static PatientCsvRow ValidRow() => new()
    {
        RawSourceId = "1",
        FirstName = "Jane",
        LastName = "Smith",
        DOB = "1990-06-15",
        Gender = "F",
        Email = "jane@example.com",
        MobileNumber = "0412345678",
        PhoneNumber = "0298765432",
        State = "NSW",
        Postcode = "2000"
    };

    private static ParsedRow<PatientCsvRow> Wrap(PatientCsvRow row) => new(row);

    private static List<ParsedRow<PatientCsvRow>> SingleList(ParsedRow<PatientCsvRow> parsedRow) => [parsedRow];

    private readonly PatientValidator _sut = new();

    [Fact]
    public void Validate_ValidRow_NoErrors()
    {
        var parsedRow = Wrap(ValidRow());
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Empty(parsedRow.Errors);
    }

    [Fact]
    public void Validate_MissingFirstName_AddsError()
    {
        var row = ValidRow();
        row.FirstName = "";
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "FirstName");
    }

    [Fact]
    public void Validate_MissingLastName_AddsError()
    {
        var row = ValidRow();
        row.LastName = "   ";
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "LastName");
    }

    [Fact]
    public void Validate_InvalidDOBFormat_AddsError()
    {
        var row = ValidRow();
        row.DOB = "15/06/1990";
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "DOB");
    }

    [Fact]
    public void Validate_FutureDOB_AddsError()
    {
        var row = ValidRow();
        row.DOB = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "DOB" && e.Message.Contains("future"));
    }

    [Fact]
    public void Validate_UnrealisticallyOldDOB_AddsError()
    {
        var row = ValidRow();
        row.DOB = DateTime.Today.AddYears(-200).ToString("yyyy-MM-dd");
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "DOB" && e.Message.Contains("old"));
    }

    [Fact]
    public void Validate_InvalidEmail_AddsError()
    {
        var row = ValidRow();
        row.Email = "not-an-email";
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "Email");
    }

    [Fact]
    public void Validate_BlankEmail_NoError()
    {
        var row = ValidRow();
        row.Email = "";
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.DoesNotContain(parsedRow.Errors, e => e.FieldName == "Email");
    }

    [Fact]
    public void Validate_InvalidMobile_AddsError()
    {
        var row = ValidRow();
        row.MobileNumber = "1234567890";
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "MobileNumber");
    }

    [Fact]
    public void Validate_InvalidPhone_AddsError()
    {
        var row = ValidRow();
        row.PhoneNumber = "123";
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "PhoneNumber");
    }

    [Fact]
    public void Validate_InvalidState_AddsError()
    {
        var row = ValidRow();
        row.State = "ZZ";
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "State");
    }

    [Fact]
    public void Validate_ValidState_CaseInsensitive_NoError()
    {
        var row = ValidRow();
        row.State = "nsw";
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.DoesNotContain(parsedRow.Errors, e => e.FieldName == "State");
    }

    [Fact]
    public void Validate_InvalidPostcode_AddsError()
    {
        var row = ValidRow();
        row.Postcode = "200";
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "Postcode");
    }

    [Fact]
    public void Validate_InvalidGender_AddsError()
    {
        var row = ValidRow();
        row.Gender = "Unknown";
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "Gender");
    }

    [Fact]
    public void Validate_InvalidateValidGenderMale_AddsError()
    {
        var row = ValidRow();
        row.Gender = "Male";
        var parsedRow = Wrap(row);
        _sut.Validate(parsedRow, SingleList(parsedRow));
		// Validator throws error for valid values like Male, Female, Others, also because it only accepts M, F, O. This is intentional to keep the validator simple.
		// Besides, auto-fix will convert valid values to M, F, O, so it's not a problem if the validator marks them as errors.
		Assert.Contains(parsedRow.Errors, e => e.FieldName == "Gender");
    }

    [Fact]
    public void Validate_CustomMaxAgeYears_RespectsOption()
    {
        var options = new PatientValidationOptions { MaxAgeYears = 50 };
        var sut = new PatientValidator(options);
        var row = ValidRow();
        row.DOB = DateTime.Today.AddYears(-60).ToString("yyyy-MM-dd");
        var parsedRow = Wrap(row);
        sut.Validate(parsedRow, SingleList(parsedRow));
        Assert.Contains(parsedRow.Errors, e => e.FieldName == "DOB" && e.Message.Contains("old"));
    }

    [Fact]
    public void Validate_DuplicateSourceId_AddsError()
    {
        var row1 = ValidRow();
        row1.RawSourceId = "1";
        row1.RowIndex = 1;

        var row2 = ValidRow();
        row2.RawSourceId = "1";
        row2.RowIndex = 2;

        var parsedRow1 = Wrap(row1);
        var parsedRow2 = Wrap(row2);
        var allRows = new List<ParsedRow<PatientCsvRow>> { parsedRow1, parsedRow2 };

        _sut.Validate(parsedRow1, allRows);
        _sut.Validate(parsedRow2, allRows);

        Assert.DoesNotContain(parsedRow1.Errors, e => e.FieldName == "ID");
        Assert.Contains(parsedRow2.Errors, e => e.FieldName == "ID" && e.Message.Contains("row 1"));
    }

    [Fact]
    public void Validate_DuplicateSourceId_IsCaseInsensitive()
    {
        var row1 = ValidRow();
        row1.RawSourceId = "ABC";
        row1.RowIndex = 1;

        var row2 = ValidRow();
        row2.RawSourceId = "abc";
        row2.RowIndex = 2;

        var parsedRow1 = Wrap(row1);
        var parsedRow2 = Wrap(row2);
        var allRows = new List<ParsedRow<PatientCsvRow>> { parsedRow1, parsedRow2 };

        _sut.Validate(parsedRow1, allRows);
        _sut.Validate(parsedRow2, allRows);

        Assert.Contains(parsedRow2.Errors, e => e.FieldName == "ID");
    }

    [Fact]
    public void Validate_UniqueSourceIds_NoDuplicateError()
    {
        var row1 = ValidRow();
        row1.RawSourceId = "1";
        row1.RowIndex = 1;

        var row2 = ValidRow();
        row2.RawSourceId = "2";
        row2.RowIndex = 2;

        var parsedRow1 = Wrap(row1);
        var parsedRow2 = Wrap(row2);
        var allRows = new List<ParsedRow<PatientCsvRow>> { parsedRow1, parsedRow2 };

        _sut.Validate(parsedRow1, allRows);
        _sut.Validate(parsedRow2, allRows);

        Assert.DoesNotContain(parsedRow1.Errors, e => e.FieldName == "ID");
        Assert.DoesNotContain(parsedRow2.Errors, e => e.FieldName == "ID");
    }
}
