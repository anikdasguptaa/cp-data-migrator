using CP.Migrator.Application.Ingestion;
using CP.Migrator.Business.AutoFix;
using CP.Migrator.Business.Export;
using CP.Migrator.Business.History;
using CP.Migrator.Business.Parser;
using CP.Migrator.Business.Validation;
using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Extensions;
using CP.Migrator.Models.Results;

namespace CP.Migrator.Application.Pipeline;

/// <summary>
/// Implements <see cref="IMigratorPipelineService"/> by delegating to the injected
/// business-layer services.  This is the only class in the Application project that
/// references business namespaces directly; all UI classes depend solely on
/// <see cref="IMigratorPipelineService"/> and the shared Models layer.
/// </summary>
internal sealed class MigratorPipelineService : IMigratorPipelineService
{
    private readonly ICsvParserService<PatientCsvRow> _patientParser;
    private readonly ICsvParserService<TreatmentCsvRow> _treatmentParser;
    private readonly IRecordValidator<PatientCsvRow> _patientValidator;
    private readonly ICrossRecordValidator<TreatmentCsvRow, PatientCsvRow> _treatmentValidator;
    private readonly IAutoFixService<PatientCsvRow> _patientAutoFix;
    private readonly IAutoFixService<TreatmentCsvRow> _treatmentAutoFix;
    private readonly IRowExportService _exportService;
    private readonly IUndoRedoManager<PatientCsvRow> _patientUndoRedo;
    private readonly IUndoRedoManager<TreatmentCsvRow> _treatmentUndoRedo;
    private readonly IIngestionService _ingestionService;

    public MigratorPipelineService(
        ICsvParserService<PatientCsvRow> patientParser,
        ICsvParserService<TreatmentCsvRow> treatmentParser,
        IRecordValidator<PatientCsvRow> patientValidator,
        ICrossRecordValidator<TreatmentCsvRow, PatientCsvRow> treatmentValidator,
        IAutoFixService<PatientCsvRow> patientAutoFix,
        IAutoFixService<TreatmentCsvRow> treatmentAutoFix,
        IRowExportService exportService,
        IUndoRedoManager<PatientCsvRow> patientUndoRedo,
        IUndoRedoManager<TreatmentCsvRow> treatmentUndoRedo,
        IIngestionService ingestionService)
    {
        _patientParser = patientParser;
        _treatmentParser = treatmentParser;
        _patientValidator = patientValidator;
        _treatmentValidator = treatmentValidator;
        _patientAutoFix = patientAutoFix;
        _treatmentAutoFix = treatmentAutoFix;
        _exportService = exportService;
        _patientUndoRedo = patientUndoRedo;
        _treatmentUndoRedo = treatmentUndoRedo;
        _ingestionService = ingestionService;
    }

    // ------------------------------------------------------------------
    //  Parse
    // ------------------------------------------------------------------

    public IReadOnlyList<ParsedRow<PatientCsvRow>> ParsePatients(string filePath)
        => _patientParser.Parse(filePath).ToList();

    public IReadOnlyList<ParsedRow<TreatmentCsvRow>> ParseTreatments(string filePath)
        => _treatmentParser.Parse(filePath).ToList();

    // ------------------------------------------------------------------
    //  Validate
    // ------------------------------------------------------------------

    public void ValidatePatients(IList<ParsedRow<PatientCsvRow>> rows)
    {
        foreach (var row in rows)
        {
            row.Errors.Clear();
            _patientValidator.Validate(row, rows);
        }
    }

    public void ValidateTreatments(
        IList<ParsedRow<TreatmentCsvRow>> rows,
        IList<ParsedRow<PatientCsvRow>> patients)
    {
        // A snapshot is intentional — the TreatmentValidator caches the patient-ID
        // HashSet keyed on reference equality of the context list.  Passing a fresh
        // snapshot ensures the cache rebuilds on every validation run so that rows
        // deleted from _patientRows are no longer treated as valid cross-references.
        var patientSnapshot = patients.ToList();
        foreach (var row in rows)
        {
            row.Errors.Clear();
            _treatmentValidator.Validate(row, rows, patientSnapshot);
        }
    }

    // ------------------------------------------------------------------
    //  Auto-fix
    // ------------------------------------------------------------------

    public int AutoFixPatients(IList<ParsedRow<PatientCsvRow>> rows)
    {
        int count = 0;
        foreach (var row in rows)
        {
            var before = row.Row.Clone();
            if (_patientAutoFix.TryFix(row.Row))
            {
                row.IsAutoFixed = true;
                count++;
                _patientUndoRedo.Push(before, row.Row.Clone());
            }
        }
        return count;
    }

    public int AutoFixTreatments(IList<ParsedRow<TreatmentCsvRow>> rows)
    {
        int count = 0;
        foreach (var row in rows)
        {
            var before = row.Row.Clone();
            if (_treatmentAutoFix.TryFix(row.Row))
            {
                row.IsAutoFixed = true;
                count++;
                _treatmentUndoRedo.Push(before, row.Row.Clone());
            }
        }
        return count;
    }

    // ------------------------------------------------------------------
    //  Undo / Redo — patients
    // ------------------------------------------------------------------

    public bool CanUndoPatient => _patientUndoRedo.CanUndo;
    public bool CanRedoPatient => _patientUndoRedo.CanRedo;

    public void PushPatientEdit(PatientCsvRow before, PatientCsvRow after)
        => _patientUndoRedo.Push(before, after);

    public PatientCsvRow UndoPatient() => _patientUndoRedo.Undo();
    public PatientCsvRow RedoPatient() => _patientUndoRedo.Redo();
    public void ClearPatientHistory() => _patientUndoRedo.Clear();

    // ------------------------------------------------------------------
    //  Undo / Redo — treatments
    // ------------------------------------------------------------------

    public bool CanUndoTreatment => _treatmentUndoRedo.CanUndo;
    public bool CanRedoTreatment => _treatmentUndoRedo.CanRedo;

    public void PushTreatmentEdit(TreatmentCsvRow before, TreatmentCsvRow after)
        => _treatmentUndoRedo.Push(before, after);

    public TreatmentCsvRow UndoTreatment() => _treatmentUndoRedo.Undo();
    public TreatmentCsvRow RedoTreatment() => _treatmentUndoRedo.Redo();
    public void ClearTreatmentHistory() => _treatmentUndoRedo.Clear();

    // ------------------------------------------------------------------
    //  Export
    // ------------------------------------------------------------------

    public int ExportInvalidPatients(IEnumerable<ParsedRow<PatientCsvRow>> rows, string outputFilePath)
        => _exportService.ExportInvalid(rows, outputFilePath);

    public int ExportInvalidTreatments(IEnumerable<ParsedRow<TreatmentCsvRow>> rows, string outputFilePath)
        => _exportService.ExportInvalid(rows, outputFilePath);

    // ------------------------------------------------------------------
    //  Ingest
    // ------------------------------------------------------------------

    public async Task<IngestionReport> IngestAsync(
        int clinicId,
        IList<ParsedRow<PatientCsvRow>> patients,
        IList<ParsedRow<TreatmentCsvRow>> treatments)
        => await _ingestionService.IngestAsync(clinicId, patients, treatments);
}
