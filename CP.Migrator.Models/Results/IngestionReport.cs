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
    /// </summary>
    public class IngestionReport
    {
        public int TotalRows { get; set; }
        public int SuccessCount => Entries.Count(e => e.Status == IngestionStatus.Inserted);
        public int SkippedCount => Entries.Count(e => e.Status == IngestionStatus.Skipped);
        public int ErrorCount => Entries.Count(e => e.Status == IngestionStatus.Failed);
        public int DuplicateCount => Entries.Count(e => e.Status == IngestionStatus.Duplicate);

        public List<IngestionReportEntry> Entries { get; } = new List<IngestionReportEntry>();
    }
}
