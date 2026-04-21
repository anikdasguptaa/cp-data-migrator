using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;
using CsvHelper;
using System.Globalization;
using System.Reflection;

namespace CP.Migrator.Business.Export
{
    /// <summary>
    /// Concrete implementation of <see cref="IRowExportService"/> that writes
    /// filtered CSV rows to disk using CsvHelper.
    /// </summary>
    internal class RowExportService : IRowExportService
    {
        // Maps internal property names (which reflect our Raw* naming convention) back
        // to the original CSV column names the operator recognises.
        private static readonly Dictionary<string, string> HeaderAliases = new()
        {
            [nameof(CsvRow.RawSourceId)] = "Id",
            ["RawPatientSourceId"] = "PatientID",
            ["DentistId"] = "DentistID",
        };

        public int ExportInvalid<TRow>(IEnumerable<ParsedRow<TRow>> rows, string outputFilePath) where TRow : CsvRow
        {
            var invalidRows = rows.Where(r => !r.IsValid).ToList();

            if (invalidRows.Count == 0)
                return 0;

            // Cache the public instance properties of TRow (exclude RowIndex from CsvRow base)
            var properties = typeof(TRow)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name != nameof(CsvRow.RowIndex))
                .ToList();

            using var writer = new StreamWriter(outputFilePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            // --- Header ---
            foreach (var prop in properties)
                csv.WriteField(HeaderAliases.GetValueOrDefault(prop.Name, prop.Name));
            csv.WriteField("Errors");
            csv.NextRecord();

            // --- Data rows ---
            foreach (var parsedRow in invalidRows)
            {
                foreach (var prop in properties)
                    csv.WriteField(prop.GetValue(parsedRow.Row));

                var errors = string.Join("; ", parsedRow.Errors.Select(e => e.ToString()));
                csv.WriteField(errors);
                csv.NextRecord();
            }

            return invalidRows.Count;
        }
    }
}
