using CP.Migrator.Business.AutoFix;
using CP.Migrator.Models.Csv;

namespace CP.Migrator.Test.Business.AutoFix;

public class PatientAutoFixTests
{
    private readonly PatientAutoFix _sut = new();

    [Fact]
    public void TryFix_NoChangesNeeded_ReturnsFalse()
    {
        var row = new PatientCsvRow
        {
            FirstName = "Jane",
            LastName = "Smith",
            DOB = "1990-06-15",
            Gender = "F",
            MobileNumber = "0412345678",
            PhoneNumber = "0298765432"
        };

        var result = _sut.TryFix(row);

        Assert.False(result);
    }

    [Fact]
    public void TryFix_TrimsWhitespace_ReturnsTrue()
    {
        var row = new PatientCsvRow { FirstName = "  Jane  ", LastName = "Smith" };
        var result = _sut.TryFix(row);
        Assert.True(result);
        Assert.Equal("Jane", row.FirstName);
    }

    [Theory]
    [InlineData("Male", "M")]
    [InlineData("MALE", "M")]
    [InlineData("male", "M")]
    [InlineData("Female", "F")]
    [InlineData("FEMALE", "F")]
    [InlineData("female", "F")]
	[InlineData("Other", "O")]
	public void TryFix_NormalizesGender_ReturnsTrue(string input, string expected)
    {
        var row = new PatientCsvRow { Gender = input };
        var result = _sut.TryFix(row);
        Assert.True(result);
        Assert.Equal(expected, row.Gender);
    }

    [Theory]
    [InlineData("M")]
    [InlineData("F")]
    public void TryFix_AlreadyNormalizedGender_ReturnsFalse(string gender)
    {
        var row = new PatientCsvRow { Gender = gender };
        var result = _sut.TryFix(row);
        Assert.False(result);
        Assert.Equal(gender, row.Gender);
    }

    [Fact]
    public void TryFix_NormalizesPhone_StripsNonDigits()
    {
        var row = new PatientCsvRow { MobileNumber = "04 12 345 678" };
        var result = _sut.TryFix(row);
        Assert.True(result);
        Assert.Equal("0412345678", row.MobileNumber);
    }

    [Fact]
    public void TryFix_NormalizesPhoneNumber_StripsNonDigits()
    {
        var row = new PatientCsvRow { PhoneNumber = "(02) 9876-5432" };
        var result = _sut.TryFix(row);
        Assert.True(result);
        Assert.Equal("0298765432", row.PhoneNumber);
    }

	[Theory]
    [InlineData("15/06/1990", "1990-06-15")]
    [InlineData("15-06-1990", "1990-06-15")]
    [InlineData("06/15/1990", "1990-06-15")]
    public void TryFix_NormalizesDOBFormat(string input, string expected)
    {
        var row = new PatientCsvRow { DOB = input };
        var result = _sut.TryFix(row);
        Assert.True(result);
        Assert.Equal(expected, row.DOB);
    }

    [Fact]
    public void TryFix_AlreadyCorrectDOBFormat_ReturnsFalse()
    {
        var row = new PatientCsvRow { DOB = "1990-06-15" };
        var result = _sut.TryFix(row);
        Assert.False(result);
        Assert.Equal("1990-06-15", row.DOB);
    }

    [Fact]
    public void TryFix_NullFields_DoNotThrow()
    {
        var row = new PatientCsvRow();
        var ex = Record.Exception(() => _sut.TryFix(row));
        Assert.Null(ex);
    }
}
