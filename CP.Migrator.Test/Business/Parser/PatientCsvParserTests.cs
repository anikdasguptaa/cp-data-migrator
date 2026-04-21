using CP.Migrator.Business.Parser;

namespace CP.Migrator.Test.Business.Parser;

public class PatientCsvParserTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly PatientCsvParser _sut = new();

    public PatientCsvParserTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.Delete(_tempDir, recursive: true);
    }

    private string WriteCsv(string content)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.csv");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Parse_ValidRow_ReturnsParsedRow()
    {
        var path = WriteCsv("""
            Id,FirstName,LastName,DOB,Gender,Email,MobileNumber,PhoneNumber,Street,Suburb,State,Postcode
            1,Jane,Smith,1990-06-15,F,jane@example.com,0412345678,0298765432,1 Main St,Sydney,NSW,2000
            """);

        var results = _sut.Parse(path).ToList();

        Assert.Single(results);
        var row = results[0].Row;
        Assert.Equal("1", row.RawSourceId);
        Assert.Equal("Jane", row.FirstName);
        Assert.Equal("Smith", row.LastName);
        Assert.Equal("1990-06-15", row.DOB);
        Assert.Equal("F", row.Gender);
        Assert.Equal("NSW", row.State);
        Assert.Equal("2000", row.Postcode);
        Assert.Empty(results[0].Errors);
    }

    [Fact]
    public void Parse_MultipleRows_ReturnsAllRows()
    {
        var path = WriteCsv("""
            Id,FirstName,LastName,DOB,Gender,Email,MobileNumber,PhoneNumber,Street,Suburb,State,Postcode
            1,Jane,Smith,1990-06-15,F,,,,,,,
            2,John,Doe,1985-01-01,M,,,,,,,
            """);

        var results = _sut.Parse(path).ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Parse_AssignsRowIndexStartingAtOne()
    {
        var path = WriteCsv("""
            Id,FirstName,LastName,DOB,Gender,Email,MobileNumber,PhoneNumber,Street,Suburb,State,Postcode
            1,Jane,Smith,1990-06-15,F,,,,,,,
            2,John,Doe,1985-01-01,M,,,,,,,
            """);

        var results = _sut.Parse(path).ToList();

        Assert.Equal(1, results[0].Row.RowIndex);
        Assert.Equal(2, results[1].Row.RowIndex);
    }

    [Fact]
    public void Parse_MissingOptionalColumns_DoesNotThrow()
    {
        // Only mandatory-ish columns, no optional address fields
        var path = WriteCsv("""
            Id,FirstName,LastName,DOB
            1,Jane,Smith,1990-06-15
            """);

        var ex = Record.Exception(() => _sut.Parse(path).ToList());

        Assert.Null(ex);
    }

    [Fact]
    public void Parse_TrimsFieldWhitespace()
    {
        var path = WriteCsv("""
            Id,FirstName,LastName,DOB,Gender,Email,MobileNumber,PhoneNumber,Street,Suburb,State,Postcode
            1, Jane , Smith ,1990-06-15,F,,,,,,,
            """);

        var results = _sut.Parse(path).ToList();

        Assert.Equal("Jane", results[0].Row.FirstName);
        Assert.Equal("Smith", results[0].Row.LastName);
    }

    [Fact]
    public void Parse_EmptyFile_WithHeaderOnly_ReturnsOneInvalidRow()
    {
        // CsvHelper attempts to read one record after the header and throws;
        // the parser catches this and returns a skeleton error row so the UI
        // can surface the problem rather than silently swallowing it.
        var path = WriteCsv("Id,FirstName,LastName,DOB,Gender,Email,MobileNumber,PhoneNumber,Street,Suburb,State,Postcode");

        var results = _sut.Parse(path).ToList();

        Assert.Single(results);
        Assert.False(results[0].IsValid);
        Assert.Contains(results[0].Errors, e => e.FieldName == "CSV");
    }
}
