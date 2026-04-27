namespace CP.Migrator.Models.Results
{
    public enum IngestionStatus
    {
        Inserted,
        Skipped,
        Failed,
        Duplicate
    }

    /// <summary>Outcome for a single CSV row during the ingestion phase.</summary>
    public class IngestionReportEntry
    {
        public int RowIndex { get; set; }
        public string RawSourceId { get; set; }
        public IngestionStatus Status { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Summary produced at the end of a full ingestion run.
    /// Displayed in the UI and can be exported as a log.
    /// Counts are set once by the ingestion service after the run completes;
    /// this class is a pure data-transfer object with no behaviour of its own.
    /// </summary>
    public class IngestionReport
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public int DuplicateCount { get; set; }

        public List<IngestionReportEntry> Entries { get; } = new List<IngestionReportEntry>();
    }
}
