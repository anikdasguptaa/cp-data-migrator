using CP.Migrator.Business.Config;
using CP.Migrator.Business.Parser;

namespace CP.Migrator.Test.Business.Parser;

public class TreatmentCsvParserTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly TreatmentCsvParser _sut = new(new CsvParserOptions());

    public TreatmentCsvParserTests()
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
            Id,PatientID,DentistID,TreatmentItem,Description,Price,Fee,Date,Paid,ToothNumber,Surface
            T1,P1,D1,011,Exam,100.00,150.00,2024-03-15,Yes,11,
            """);

        var results = _sut.Parse(path).ToList();

        Assert.Single(results);
        var row = results[0].Row;
        Assert.Equal("T1", row.RawSourceId);
        Assert.Equal("P1", row.RawPatientSourceId);
        Assert.Equal("011", row.TreatmentItem);
        Assert.Equal("150.00", row.Fee);
        Assert.Equal("2024-03-15", row.Date);
        Assert.Equal("Yes", row.Paid);
        Assert.Empty(results[0].Errors);
    }

    [Fact]
    public void Parse_MultipleRows_ReturnsAllRows()
    {
        var path = WriteCsv("""
            Id,PatientID,DentistID,TreatmentItem,Description,Price,Fee,Date,Paid,ToothNumber,Surface
            T1,P1,D1,011,Exam,100.00,150.00,2024-03-15,Yes,,
            T2,P2,D1,022,Clean,80.00,120.00,2024-03-16,No,,
            """);

        var results = _sut.Parse(path).ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Parse_AssignsRowIndexStartingAtOne()
    {
        var path = WriteCsv("""
            Id,PatientID,DentistID,TreatmentItem,Description,Price,Fee,Date,Paid,ToothNumber,Surface
            T1,P1,D1,011,Exam,100.00,150.00,2024-03-15,Yes,,
            T2,P2,D1,022,Clean,80.00,120.00,2024-03-16,No,,
            """);

        var results = _sut.Parse(path).ToList();

        Assert.Equal(1, results[0].Row.RowIndex);
        Assert.Equal(2, results[1].Row.RowIndex);
    }

    [Fact]
    public void Parse_MissingOptionalColumns_DoesNotThrow()
    {
        // Minimal columns only — ToothNumber and Surface omitted
        var path = WriteCsv("""
            Id,PatientID,TreatmentItem,Description,Fee,Date,Paid
            T1,P1,011,Exam,150.00,2024-03-15,Yes
            """);

        var ex = Record.Exception(() => _sut.Parse(path).ToList());

        Assert.Null(ex);
    }

    [Fact]
    public void Parse_TrimsFieldWhitespace()
    {
        var path = WriteCsv("""
            Id,PatientID,DentistID,TreatmentItem,Description,Price,Fee,Date,Paid,ToothNumber,Surface
            T1, P1 ,D1, 011 , Exam ,100.00,150.00,2024-03-15,Yes,,
            """);

        var results = _sut.Parse(path).ToList();

        Assert.Equal("P1", results[0].Row.RawPatientSourceId);
        Assert.Equal("011", results[0].Row.TreatmentItem);
        Assert.Equal("Exam", results[0].Row.Description);
    }

    [Fact]
    public void Parse_EmptyFile_WithHeaderOnly_ReturnsOneInvalidRow()
    {
        // CsvHelper attempts to read one record after the header and throws;
        // the parser catches this and returns a skeleton error row so the UI
        // can surface the problem rather than silently swallowing it.
        var path = WriteCsv("Id,PatientID,DentistID,TreatmentItem,Description,Price,Fee,Date,Paid,ToothNumber,Surface");

        var results = _sut.Parse(path).ToList();

        Assert.Single(results);
        Assert.False(results[0].IsValid);
        Assert.Contains(results[0].Errors, e => e.FieldName == "CSV");
    }

    [Fact]
    public void Parse_FeeKeptAsRawString_NotConverted()
    {
        var path = WriteCsv("""
            Id,PatientID,DentistID,TreatmentItem,Description,Price,Fee,Date,Paid,ToothNumber,Surface
            T1,P1,D1,011,Exam,100.00,not-a-number,2024-03-15,Yes,,
            """);

        var results = _sut.Parse(path).ToList();

        // Parser should not throw — fee conversion is deferred to validator
        Assert.Single(results);
        Assert.Equal("not-a-number", results[0].Row.Fee);
    }
}
