using CP.Migrator.Business.Export;
using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;

namespace CP.Migrator.Test.Business.Export;

public class RowExportServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly RowExportService _sut = new();

    public RowExportServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.Delete(_tempDir, recursive: true);
    }

    private string TempFile() => Path.Combine(_tempDir, $"{Guid.NewGuid()}.csv");

    private static ParsedRow<PatientCsvRow> ValidRow(string id = "1") =>
        new(new PatientCsvRow { RawSourceId = id, FirstName = "Jane", LastName = "Smith", DOB = "1990-01-01" });

    private static ParsedRow<PatientCsvRow> InvalidRow(string id = "2")
    {
        var row = new PatientCsvRow { RawSourceId = id };
        var parsedRow = new ParsedRow<PatientCsvRow>(row);
        parsedRow.Errors.Add(new ValidationError("FirstName", "First name is required."));
        return parsedRow;
    }

    [Fact]
    public void ExportInvalid_AllValid_ReturnsZero_DoesNotCreateFile()
    {
        var output = TempFile();
        var rows = new[] { ValidRow() };

        var count = _sut.ExportInvalid(rows, output);

        Assert.Equal(0, count);
        Assert.False(File.Exists(output));
    }

    [Fact]
    public void ExportInvalid_OneInvalidRow_ReturnsOne()
    {
        var output = TempFile();
        var rows = new[] { ValidRow(), InvalidRow() };

        var count = _sut.ExportInvalid(rows, output);

        Assert.Equal(1, count);
    }

    [Fact]
    public void ExportInvalid_WritesHeaderAndDataRow()
    {
        var output = TempFile();
        var parsedRow = InvalidRow("99");

        _sut.ExportInvalid([parsedRow], output);

        var lines = File.ReadAllLines(output);
        Assert.True(lines.Length >= 2, "Expected at least a header and one data row.");
        Assert.Contains("Errors", lines[0]);
        Assert.Contains("First name is required.", lines[1]);
    }

    [Fact]
    public void ExportInvalid_MultipleInvalidRows_WritesAll()
    {
        var output = TempFile();
        var rows = new[] { InvalidRow("1"), InvalidRow("2"), InvalidRow("3") };

        var count = _sut.ExportInvalid(rows, output);

        Assert.Equal(3, count);
        var lines = File.ReadAllLines(output);
        // 1 header + 3 data rows
        Assert.Equal(4, lines.Length);
    }

    [Fact]
    public void ExportInvalid_MixedRows_ExportsOnlyInvalid()
    {
        var output = TempFile();
        var rows = new[] { ValidRow("1"), InvalidRow("2"), ValidRow("3"), InvalidRow("4") };

        var count = _sut.ExportInvalid(rows, output);

        Assert.Equal(2, count);
        var lines = File.ReadAllLines(output);
        Assert.Equal(3, lines.Length); // 1 header + 2 data rows
    }
}
