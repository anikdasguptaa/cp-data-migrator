namespace CP.Migrator.Models.Csv
{
    /// <summary>
    /// Base class for all CSV row models.
    /// Carries the original source line number and the raw CSV identifier
    /// so errors can be reported back to the user against a specific row.
    /// </summary>
    public abstract class CsvRow
    {
        /// <summary>1-based line number in the source CSV file (excluding the header).</summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// The raw value from the CSV Id column, kept as a string because CSV identifiers
        /// are not guaranteed to be numeric or unique across databases.
        /// Never inserted as a primary key — used only for cross-referencing within the import session.
        /// </summary>
        public string RawSourceId { get; set; }
    }
}
