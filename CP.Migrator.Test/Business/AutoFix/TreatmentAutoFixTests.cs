using CP.Migrator.Business.AutoFix;
using CP.Migrator.Models.Csv;

namespace CP.Migrator.Test.Business.AutoFix;

public class TreatmentAutoFixTests
{
    private readonly TreatmentAutoFix _sut = new();

    [Fact]
    public void TryFix_NoChangesNeeded_ReturnsFalse()
    {
        var row = new TreatmentCsvRow
        {
            TreatmentItem = "011",
            Description = "Exam",
            Fee = "150.00",
            Date = "2024-03-15",
            Paid = "Yes"
        };

        var result = _sut.TryFix(row);

        Assert.False(result);
    }

    [Fact]
    public void TryFix_TrimsWhitespace_ReturnsTrue()
    {
        var row = new TreatmentCsvRow { TreatmentItem = "  011  " };
        var result = _sut.TryFix(row);
        Assert.True(result);
        Assert.Equal("011", row.TreatmentItem);
    }

    [Theory]
    [InlineData("15/03/2024", "2024-03-15")]
    [InlineData("15-03-2024", "2024-03-15")]
    [InlineData("03/15/2024", "2024-03-15")]
    public void TryFix_NormalizesDateFormat(string input, string expected)
    {
        var row = new TreatmentCsvRow { Date = input };
        var result = _sut.TryFix(row);
        Assert.True(result);
        Assert.Equal(expected, row.Date);
    }

    [Fact]
    public void TryFix_AlreadyCorrectDateFormat_ReturnsFalse()
    {
        var row = new TreatmentCsvRow { Date = "2024-03-15" };
        var result = _sut.TryFix(row);
        Assert.False(result);
        Assert.Equal("2024-03-15", row.Date);
    }

    [Theory]
    [InlineData("yes", "Yes")]
    [InlineData("YES", "Yes")]
    [InlineData("no", "No")]
    [InlineData("NO", "No")]
    public void TryFix_NormalizesPaid_ReturnsTrue(string input, string expected)
    {
        var row = new TreatmentCsvRow { Paid = input };
        var result = _sut.TryFix(row);
        Assert.True(result);
        Assert.Equal(expected, row.Paid);
    }

    [Theory]
    [InlineData("Yes")]
    [InlineData("No")]
    public void TryFix_AlreadyNormalizedPaid_ReturnsFalse(string paid)
    {
        var row = new TreatmentCsvRow { Paid = paid };
        var result = _sut.TryFix(row);
        Assert.False(result);
        Assert.Equal(paid, row.Paid);
    }

    [Fact]
    public void TryFix_NullFields_DoNotThrow()
    {
        var row = new TreatmentCsvRow();
        var ex = Record.Exception(() => _sut.TryFix(row));
        Assert.Null(ex);
    }

    [Fact]
    public void TryFix_MultipleFieldsFixed_ReturnsTrue()
    {
        var row = new TreatmentCsvRow
        {
            TreatmentItem = "  011  ",
            Date = "15/03/2024",
            Paid = "YES"
        };

        var result = _sut.TryFix(row);

        Assert.True(result);
        Assert.Equal("011", row.TreatmentItem);
        Assert.Equal("2024-03-15", row.Date);
        Assert.Equal("Yes", row.Paid);
    }
}
