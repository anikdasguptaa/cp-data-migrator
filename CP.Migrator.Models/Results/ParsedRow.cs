using CP.Migrator.Models.Csv;

namespace CP.Migrator.Models.Results
{
    /// <summary>
    /// Wraps a parsed CSV row with its validation state.
    /// Flows through the entire pipeline: Parser → AutoFix → Validator → Ingestion.
    /// </summary>
    public class ParsedRow<TRow> where TRow : CsvRow
    {
        public TRow Row { get; set; }

        public List<ValidationError> Errors { get; } = new List<ValidationError>();

        /// <summary>True when there are no Error-severity validation problems.</summary>
        public bool IsValid => Errors.All(e => e.Severity != ValidationSeverity.Error);

        /// <summary>Flagged by the auto-fix service when one or more field values were corrected.</summary>
        public bool IsAutoFixed { get; set; }

        public ParsedRow(TRow row)
        {
            Row = row;
        }
    }
}
