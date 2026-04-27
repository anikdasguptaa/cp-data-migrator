using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;

namespace CP.Migrator.Application.Pipeline;

/// <summary>
/// Facade over all business pipeline operations required by the UI.
/// Defines exactly the operations <see cref="Forms.MainForm"/> needs —
/// parse, validate, auto-fix, undo/redo, export, and ingest — expressed
/// in terms of the shared Models layer only.  No business namespaces
/// leak into the form itself.
/// </summary>
public interface IMigratorPipelineService
{
    // ------------------------------------------------------------------
    //  Parse
    // ------------------------------------------------------------------

    /// <summary>Parses a patient CSV file and returns one row per data line.</summary>
    IReadOnlyList<ParsedRow<PatientCsvRow>> ParsePatients(string filePath);

    /// <summary>Parses a treatment CSV file and returns one row per data line.</summary>
    IReadOnlyList<ParsedRow<TreatmentCsvRow>> ParseTreatments(string filePath);

    // ------------------------------------------------------------------
    //  Validate
    // ------------------------------------------------------------------

    /// <summary>Validates every patient row in <paramref name="rows"/> in-place.</summary>
    void ValidatePatients(IList<ParsedRow<PatientCsvRow>> rows);

    /// <summary>
    /// Validates every treatment row in <paramref name="rows"/> against
    /// <paramref name="patients"/> for cross-reference checks.
    /// </summary>
    void ValidateTreatments(
        IList<ParsedRow<TreatmentCsvRow>> rows,
        IList<ParsedRow<PatientCsvRow>> patients);

    // ------------------------------------------------------------------
    //  Auto-fix
    // ------------------------------------------------------------------

    /// <summary>
    /// Applies automatic corrections to all patient rows in-place.
    /// Returns the number of rows that were modified.
    /// </summary>
    int AutoFixPatients(IList<ParsedRow<PatientCsvRow>> rows);

    /// <summary>
    /// Applies automatic corrections to all treatment rows in-place.
    /// Returns the number of rows that were modified.
    /// </summary>
    int AutoFixTreatments(IList<ParsedRow<TreatmentCsvRow>> rows);

    // ------------------------------------------------------------------
    //  Undo / Redo — patients
    // ------------------------------------------------------------------

    bool CanUndoPatient { get; }
    bool CanRedoPatient { get; }

    /// <summary>Snapshots the current patient row state before an edit.</summary>
    void PushPatientEdit(PatientCsvRow before, PatientCsvRow after);

    /// <summary>Reverts the most recent patient edit and returns the before-snapshot.</summary>
    PatientCsvRow UndoPatient();

    /// <summary>Re-applies the most recently undone patient edit and returns the after-snapshot.</summary>
    PatientCsvRow RedoPatient();

    /// <summary>Clears all patient undo/redo history.</summary>
    void ClearPatientHistory();

    // ------------------------------------------------------------------
    //  Undo / Redo — treatments
    // ------------------------------------------------------------------

    bool CanUndoTreatment { get; }
    bool CanRedoTreatment { get; }

    /// <summary>Snapshots the current treatment row state before an edit.</summary>
    void PushTreatmentEdit(TreatmentCsvRow before, TreatmentCsvRow after);

    /// <summary>Reverts the most recent treatment edit and returns the before-snapshot.</summary>
    TreatmentCsvRow UndoTreatment();

    /// <summary>Re-applies the most recently undone treatment edit and returns the after-snapshot.</summary>
    TreatmentCsvRow RedoTreatment();

    /// <summary>Clears all treatment undo/redo history.</summary>
    void ClearTreatmentHistory();

    // ------------------------------------------------------------------
    //  Export
    // ------------------------------------------------------------------

    /// <summary>Writes invalid patient rows to a CSV file. Returns the number of rows written.</summary>
    int ExportInvalidPatients(IEnumerable<ParsedRow<PatientCsvRow>> rows, string outputFilePath);

    /// <summary>Writes invalid treatment rows to a CSV file. Returns the number of rows written.</summary>
    int ExportInvalidTreatments(IEnumerable<ParsedRow<TreatmentCsvRow>> rows, string outputFilePath);

    // ------------------------------------------------------------------
    //  Ingest
    // ------------------------------------------------------------------

    /// <summary>
    /// Ingests all valid rows into the target clinic.  Uses a DI scope internally
    /// so the scoped <c>IIngestionService</c> lifetime is respected per call.
    /// </summary>
    Task<IngestionReport> IngestAsync(
        int clinicId,
        IList<ParsedRow<PatientCsvRow>> patients,
        IList<ParsedRow<TreatmentCsvRow>> treatments);
}
