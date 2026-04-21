using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;

namespace CP.Migrator.Business.Ingestion
{
    /// <summary>
    /// Orchestrates the final ingestion step of the import pipeline.
    /// <para>
    /// <strong>Full pipeline contract (UI layer responsibility):</strong>
    /// <list type="number">
    ///   <item><description><c>Parse</c> — load CSV rows into <see cref="ParsedRow{TRow}"/> instances.</description></item>
    ///   <item><description><c>Validate</c> — run validators immediately after parsing so the user
    ///       sees the full set of errors on first load. Some errors will suggest "Try auto-fix".</description></item>
    ///   <item><description><c>Auto-fix (user-triggered)</c> — the user reviews errors and clicks
    ///       Auto-fix. <see cref="IAutoFixService{TRow}.TryFix"/> mutates rows in-place.</description></item>
    ///   <item><description><c>Re-validate</c> — run validators again after auto-fix so the UI
    ///       reflects what was resolved and what still needs manual correction.</description></item>
    ///   <item><description><c>Ingest</c> — call <see cref="IngestAsync"/> once the user is satisfied.
    ///       Only rows where <see cref="ParsedRow{TRow}.IsValid"/> is <c>true</c> are persisted;
    ///       invalid rows are recorded as <see cref="IngestionStatus.Skipped"/>.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Internally, ingestion maps valid <see cref="PatientCsvRow"/> records to <c>tblPatient</c>,
    /// then groups valid <see cref="TreatmentCsvRow"/> records by (PatientId, Date) to produce
    /// one Invoice per group, then maps treatments and line items — all within a single
    /// database transaction. An <see cref="IngestionReport"/> summarising inserts, duplicates,
    /// skips, and failures is returned.
    /// </para>
    /// </summary>
    public interface IIngestionService
    {
        Task<IngestionReport> IngestAsync(
            int clinicId,
            IEnumerable<ParsedRow<PatientCsvRow>> patientRows,
            IEnumerable<ParsedRow<TreatmentCsvRow>> treatmentRows);
    }
}
