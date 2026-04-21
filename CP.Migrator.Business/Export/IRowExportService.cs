using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;

namespace CP.Migrator.Business.Export
{
    /// <summary>
    /// Provides row-level CSV export operations.
    /// </summary>
    public interface IRowExportService
    {
        /// <summary>
        /// Filters <paramref name="rows"/> to those that are invalid and writes them
        /// to <paramref name="outputFilePath"/> with an appended Errors column.
        /// Returns the number of rows written.
        /// </summary>
        int ExportInvalid<TRow>(IEnumerable<ParsedRow<TRow>> rows, string outputFilePath) where TRow : CsvRow;
    }
}
