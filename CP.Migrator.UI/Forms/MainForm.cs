using System.ComponentModel;
using CP.Migrator.Business.AutoFix;
using CP.Migrator.Business.Export;
using CP.Migrator.Business.History;
using CP.Migrator.Business.Ingestion;
using CP.Migrator.Business.Parser;
using CP.Migrator.Business.Validation;
using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;
using CP.Migrator.UI.Helpers;
using CP.Migrator.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CP.Migrator.UI.Forms;

/// <summary>
/// Application shell.  Hosts the patient and treatment grids and orchestrates
/// the full import pipeline: Load → Validate → Auto-Fix → Ingest → Report.
/// All business work is delegated to injected services; this class only handles
/// UI concerns (data-binding, button state, row colouring, dialogs).
/// </summary>
internal partial class MainForm : Form
{
    // Injected services
    private readonly ICsvParserService<PatientCsvRow> _patientParser;
    private readonly ICsvParserService<TreatmentCsvRow> _treatmentParser;
    private readonly IRecordValidator<PatientCsvRow> _patientValidator;
    private readonly ICrossRecordValidator<TreatmentCsvRow, PatientCsvRow> _treatmentValidator;
    private readonly IAutoFixService<PatientCsvRow> _patientAutoFix;
    private readonly IAutoFixService<TreatmentCsvRow> _treatmentAutoFix;
    private readonly IRowExportService _exportService;
    private readonly IUndoRedoManager<PatientCsvRow> _patientUndoRedo;
    private readonly IUndoRedoManager<TreatmentCsvRow> _treatmentUndoRedo;
    private readonly IServiceProvider _serviceProvider;

    // In-memory pipeline state
    private List<ParsedRow<PatientCsvRow>> _patientRows = [];
    private List<ParsedRow<TreatmentCsvRow>> _treatmentRows = [];

    // Bound directly to the DataGridViews — rebuilt on every full refresh.
    private BindingList<PatientRowItem> _patientItems = [];
    private BindingList<TreatmentRowItem> _treatmentItems = [];

    // Tracks whether a file has ever been loaded — drives the empty-state stats label.
    private bool _patientFileLoaded;
    private bool _treatmentFileLoaded;

    // Pipeline-state flags that drive Validate / AutoFix button availability.
    // _validationDirty: data has changed since the last validation run → Validate should be clickable.
    // _autoFixReady:    validation has been run on current data → AutoFix is meaningful.
    private bool _validationDirty;
    private bool _autoFixReady;

    // Cell-edit undo snapshots
    private PatientCsvRow? _patientEditSnapshot;
    private TreatmentCsvRow? _treatmentEditSnapshot;

    // Row colours
    private static readonly Color ColorError = Color.FromArgb(255, 210, 210);
    private static readonly Color ColorWarning = Color.FromArgb(255, 245, 195);
    private static readonly Color ColorAutoFixed = Color.FromArgb(205, 230, 255);
    private static readonly Color ColorValid = Color.White;

    // Result-column accent (Status / Errors) — header tint and cell tint
    private static readonly Color ColorResultHeader = Color.FromArgb(220, 220, 220);
    private static readonly Color ColorResultCell   = Color.FromArgb(240, 240, 240);

    // --------------------------------------------------------------------------
    /// <summary>
    /// Initialises the form, builds grid columns, and wires all UI events.
    /// All business dependencies are resolved by the DI container and injected here;
    /// the form itself never interacts with service registrations directly.
    /// </summary>
    /// <param name="patientParser">Parses patient CSV files into <see cref="PatientCsvRow"/> records.</param>
    /// <param name="treatmentParser">Parses treatment CSV files into <see cref="TreatmentCsvRow"/> records.</param>
    /// <param name="patientValidator">Validates individual patient rows against schema rules.</param>
    /// <param name="treatmentValidator">Cross-validates treatment rows against the loaded patient set.</param>
    /// <param name="patientAutoFix">Applies automatic corrections to patient rows (trim, format normalisation, etc.).</param>
    /// <param name="treatmentAutoFix">Applies automatic corrections to treatment rows.</param>
    /// <param name="exportService">Writes invalid rows to a CSV file for external correction.</param>
    /// <param name="patientUndoRedo">Tracks patient row edit and auto-fix history for undo/redo.</param>
    /// <param name="treatmentUndoRedo">Tracks treatment row edit and auto-fix history for undo/redo.</param>
    /// <param name="serviceProvider">Root service provider used to create a scoped <see cref="IIngestionService"/> per ingest run.</param>
    public MainForm(
        ICsvParserService<PatientCsvRow> patientParser,
        ICsvParserService<TreatmentCsvRow> treatmentParser,
        IRecordValidator<PatientCsvRow> patientValidator,
        ICrossRecordValidator<TreatmentCsvRow, PatientCsvRow> treatmentValidator,
        IAutoFixService<PatientCsvRow> patientAutoFix,
        IAutoFixService<TreatmentCsvRow> treatmentAutoFix,
        IRowExportService exportService,
        IUndoRedoManager<PatientCsvRow> patientUndoRedo,
        IUndoRedoManager<TreatmentCsvRow> treatmentUndoRedo,
        IServiceProvider serviceProvider)
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
        _serviceProvider = serviceProvider;

        InitializeComponent();
        BuildPatientGridColumns();
        BuildTreatmentGridColumns();
        WireEvents();
        UpdateButtons();

        // Defer column header sizing until after the form is fully rendered so
        // the grid's Font is resolved before we measure text.
        Load += (_, _) =>
        {
            AutoSizeColumnsToHeader(_patientGrid);
            AutoSizeColumnsToHeader(_treatmentGrid);
        };
    }

	// --------------------------------------------------------------------------
	//  Grid column builders
	// --------------------------------------------------------------------------

	private void BuildPatientGridColumns()
	{
		_patientGrid.Columns.AddRange(
			RoCol("RowIndex", "#", 40),
			RoCol("RawSourceId", "Id", 80),
			EdCol("FirstName", "FirstName", 110),
			EdCol("LastName", "LastName", 110),
			EdCol("DOB", "DOB", 95),
			EdCol("Gender", "Gender", 65),
			EdCol("Email", "Email", 185),
			EdCol("MobileNumber", "MobileNumber", 115),
			EdCol("PhoneNumber", "PhoneNumber", 115),
			EdCol("Street", "Street", 140),
			EdCol("Suburb", "Suburb", 110),
			EdCol("State", "State", 55),
			EdCol("Postcode", "Postcode", 75),
			StatusCol(),
			ErrorCol()
		);
	}

    private void BuildTreatmentGridColumns()
    {
        _treatmentGrid.Columns.AddRange(
            RoCol("RowIndex", "#", 40),
            RoCol("RawSourceId", "Id", 80),
            RoCol("RawPatientSourceId", "PatientID", 105),
            EdCol("DentistId", "DentistID", 90),
            EdCol("TreatmentItem", "TreatmentItem", 105),
            EdCol("Description", "Description", 180),
            EdCol("Price", "Price", 75),
            EdCol("Fee", "Fee", 75),
            EdCol("Date", "Date", 95),
            EdCol("Paid", "Paid", 60),
            EdCol("ToothNumber", "ToothNumber", 65),
            EdCol("Surface", "Surface", 70),
            StatusCol(),
            ErrorCol()
        );
    }

    private static DataGridViewTextBoxColumn RoCol(string prop, string header, int width) => new()
        { DataPropertyName = prop, HeaderText = header, Width = width, ReadOnly = true };

    private static DataGridViewTextBoxColumn EdCol(string prop, string header, int width) => new()
        { DataPropertyName = prop, HeaderText = header, Width = width };

    private static DataGridViewTextBoxColumn StatusCol()
    {
        var col = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "StatusBadge",
            HeaderText = "Status",
            Width = 85,
            ReadOnly = true,
        };
        col.DefaultCellStyle.BackColor = ColorResultCell;
        col.HeaderCell.Style.BackColor = ColorResultHeader;
        return col;
    }

    private static DataGridViewTextBoxColumn ErrorCol()
    {
        var col = new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ErrorSummary",
            HeaderText = "Errors",
            MinimumWidth = 200,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ReadOnly = true,
        };
        col.DefaultCellStyle.BackColor = ColorResultCell;
        col.HeaderCell.Style.BackColor = ColorResultHeader;
        return col;
    }

	// --------------------------------------------------------------------------
	//  Event wiring
	// --------------------------------------------------------------------------

	private void WireEvents()
    {
        _btnLoadPatient.Click += (_, _) => LoadPatientCsv();
        _btnLoadTreatment.Click += (_, _) => LoadTreatmentCsv();
        _btnValidate.Click += (_, _) => ValidateAll();
        _btnAutoFix.Click += (_, _) => AutoFixAll();
        _btnUndo.Click += (_, _) => PerformUndo();
        _btnRedo.Click += (_, _) => PerformRedo();
        _btnIngest.Click += async (_, _) => await IngestAsync();
        _btnExportInvalid.Click += (_, _) => ExportInvalid();

        _btnDeleteSelected.Click += (_, _) => DeleteSelectedRows();

        _chkPatientFilterInvalid.CheckedChanged += (_, _) => RefreshPatientGrid();
        _chkTreatmentFilterInvalid.CheckedChanged += (_, _) => RefreshTreatmentGrid();

        // Cell editing — patient grid
        _patientGrid.CellBeginEdit += PatientGrid_CellBeginEdit;
        _patientGrid.CellEndEdit += PatientGrid_CellEndEdit;
        _patientGrid.CellFormatting += Grid_CellFormatting;
        _patientGrid.SelectionChanged += (_, _) => UpdateButtons();

        // Cell editing — treatment grid
        _treatmentGrid.CellBeginEdit += TreatmentGrid_CellBeginEdit;
        _treatmentGrid.CellEndEdit += TreatmentGrid_CellEndEdit;
        _treatmentGrid.CellFormatting += Grid_CellFormatting;
        _treatmentGrid.SelectionChanged += (_, _) => UpdateButtons();

        // Keep undo/redo buttons fresh when tab changes
        _tabControl.SelectedIndexChanged += (_, _) => UpdateButtons();
    }

	// --------------------------------------------------------------------------
	//  Load CSVs
	// --------------------------------------------------------------------------

	private void LoadPatientCsv()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select patient.csv",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = "patient.csv",
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            SetStatus("Loading patient CSV…");
            _patientRows = _patientParser.Parse(dlg.FileName).ToList();
            _patientFileLoaded = true;
            _patientUndoRedo.Clear();
            RunPatientValidation();
            MarkValidated();
            RefreshPatientGrid();
            UpdateButtons();
            SetStatus($"Loaded {_patientRows.Count} patient rows from \"{Path.GetFileName(dlg.FileName)}\".");
        }
        catch (Exception ex) { ShowError("Failed to load patient CSV", ex); }
    }

    private void LoadTreatmentCsv()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select treatment.csv",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = "treatment.csv",
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            SetStatus("Loading treatment CSV…");
            _treatmentRows = _treatmentParser.Parse(dlg.FileName).ToList();
            _treatmentFileLoaded = true;
            _treatmentUndoRedo.Clear();
            RunTreatmentValidation();
            MarkValidated();
            RefreshTreatmentGrid();
            UpdateButtons();
            SetStatus($"Loaded {_treatmentRows.Count} treatment rows from \"{Path.GetFileName(dlg.FileName)}\".");
        }
        catch (Exception ex) { ShowError("Failed to load treatment CSV", ex); }
    }

	// --------------------------------------------------------------------------
	//  Validate
	// --------------------------------------------------------------------------

	private void ValidateAll()
	{
		RunPatientValidation();
		RunTreatmentValidation();
		MarkValidated();
		RefreshPatientGrid();
		RefreshTreatmentGrid();
		UpdateButtons();
		SetStatus("Validation complete.");
	}

    private void RunPatientValidation()
    {
        foreach (var row in _patientRows)
        {
            row.Errors.Clear();
            _patientValidator.Validate(row, _patientRows);
        }
    }

    private void RunTreatmentValidation()
    {
        // A new list reference is intentional: TreatmentValidator caches the patient-ID
        // HashSet keyed on reference equality of the context list. Passing the same
        // _patientRows reference after an in-place Remove() would leave the cache stale
        // and cause deleted patients to still be treated as valid cross-references.
        // Creating a snapshot here guarantees the cache rebuilds on every validation run.
        var patientSnapshot = _patientRows.ToList();
        foreach (var row in _treatmentRows)
        {
            row.Errors.Clear();
            _treatmentValidator.Validate(row, _treatmentRows, patientSnapshot);
        }
    }

    // --------------------------------------------------------------------------
    //  Auto-Fix
    // --------------------------------------------------------------------------

    private void AutoFixAll()
    {
        int fixedCount = 0;

        foreach (var row in _patientRows)
        {
            var before = row.Row.Clone();
            if (_patientAutoFix.TryFix(row.Row))
            {
                row.IsAutoFixed = true;
                fixedCount++;
                _patientUndoRedo.Push(before, row.Row.Clone());
            }
        }

        foreach (var row in _treatmentRows)
        {
            var before = row.Row.Clone();
            if (_treatmentAutoFix.TryFix(row.Row))
            {
                row.IsAutoFixed = true;
                fixedCount++;
                _treatmentUndoRedo.Push(before, row.Row.Clone());
            }
        }

        RunPatientValidation();
        RunTreatmentValidation();

        // Do NOT call MarkValidated() here — that would re-enable Auto-Fix for
        // any rows that still have errors, causing repeated no-op clicks.
        // Instead, mark validation as current but lock Auto-Fix until the user
        // makes an actual data change (cell edit, undo/redo, delete, or load).
        _validationDirty = false;
        _autoFixReady    = false;

        RefreshPatientGrid();
        RefreshTreatmentGrid();
        UpdateButtons();
        SetStatus($"Auto-fix applied {fixedCount} correction(s). Validation re-run.");
    }

    // --------------------------------------------------------------------------
    //  Undo / Redo
    // --------------------------------------------------------------------------

    private void PerformUndo()
    {
        bool did = false;

        if (_tabControl.SelectedTab == _patientTab && _patientUndoRedo.CanUndo)
        {
            ApplyPatientSnapshot(_patientUndoRedo.Undo());
            did = true;
        }
        else if (_tabControl.SelectedTab == _treatmentTab && _treatmentUndoRedo.CanUndo)
        {
            ApplyTreatmentSnapshot(_treatmentUndoRedo.Undo());
            did = true;
        }

        if (did) { MarkDataChanged(); RefreshPatientGrid(); RefreshTreatmentGrid(); UpdateButtons(); SetStatus("Undo applied."); }
    }

    private void PerformRedo()
    {
        bool did = false;

        if (_tabControl.SelectedTab == _patientTab && _patientUndoRedo.CanRedo)
        {
            ApplyPatientSnapshot(_patientUndoRedo.Redo());
            did = true;
        }
        else if (_tabControl.SelectedTab == _treatmentTab && _treatmentUndoRedo.CanRedo)
        {
            ApplyTreatmentSnapshot(_treatmentUndoRedo.Redo());
            did = true;
        }

        if (did) { MarkDataChanged(); RefreshPatientGrid(); RefreshTreatmentGrid(); UpdateButtons(); SetStatus("Redo applied."); }
    }

    private void ApplyPatientSnapshot(PatientCsvRow snapshot)
    {
        var target = _patientRows.FirstOrDefault(r => r.Row.RowIndex == snapshot.RowIndex);
        if (target == null) return;
        snapshot.CopyTo(target.Row);
        target.IsAutoFixed = false;
        target.Errors.Clear();
        _patientValidator.Validate(target, _patientRows);
        RunTreatmentValidation();
    }

    private void ApplyTreatmentSnapshot(TreatmentCsvRow snapshot)
    {
        var target = _treatmentRows.FirstOrDefault(r => r.Row.RowIndex == snapshot.RowIndex);
        if (target == null) return;
        snapshot.CopyTo(target.Row);
        target.IsAutoFixed = false;
        target.Errors.Clear();
        _treatmentValidator.Validate(target, _treatmentRows, _patientRows);
    }

    // --------------------------------------------------------------------------
    //  Cell editing — patient
    // --------------------------------------------------------------------------

    private void PatientGrid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (PatientItemAt(e.RowIndex) is PatientRowItem item)
            _patientEditSnapshot = item.ParsedRow.Row.Clone();
    }

    private void PatientGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_patientEditSnapshot == null) return;
        if (PatientItemAt(e.RowIndex) is PatientRowItem item)
        {
            _patientUndoRedo.Push(_patientEditSnapshot, item.ParsedRow.Row.Clone());
            item.ParsedRow.Errors.Clear();
            _patientValidator.Validate(item.ParsedRow, _patientRows);
            RunTreatmentValidation();
            MarkDataChanged();
            _patientGrid.InvalidateRow(e.RowIndex);
            RefreshTreatmentGrid();
            UpdatePatientStats();
            UpdateButtons();
        }
        _patientEditSnapshot = null;
    }

    // --------------------------------------------------------------------------
    //  Cell editing — treatment
    // --------------------------------------------------------------------------

    private void TreatmentGrid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (TreatmentItemAt(e.RowIndex) is TreatmentRowItem item)
            _treatmentEditSnapshot = item.ParsedRow.Row.Clone();
    }

    private void TreatmentGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_treatmentEditSnapshot == null) return;
        if (TreatmentItemAt(e.RowIndex) is TreatmentRowItem item)
        {
            _treatmentUndoRedo.Push(_treatmentEditSnapshot, item.ParsedRow.Row.Clone());
            item.ParsedRow.Errors.Clear();
            _treatmentValidator.Validate(item.ParsedRow, _treatmentRows, _patientRows);
            MarkDataChanged();
            _treatmentGrid.InvalidateRow(e.RowIndex);
            UpdateTreatmentStats();
            UpdateButtons();
        }
        _treatmentEditSnapshot = null;
    }

    // --------------------------------------------------------------------------
    //  Row colouring
    // --------------------------------------------------------------------------

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (sender is not DataGridView grid || e.RowIndex < 0) return;
        if (grid.Rows[e.RowIndex].DataBoundItem is not IRowItem item) return;

        // Result columns (Status / Errors) always keep the grey tint — they are
        // read-only validation output, not imported data, so their colour must
        // stay consistent regardless of the row's validation state.
        bool isResultColumn = grid.Columns[e.ColumnIndex].DataPropertyName
            is "StatusBadge" or "ErrorSummary";

        if (isResultColumn)
        {
            e.CellStyle.BackColor = ColorResultCell;
            e.CellStyle.SelectionBackColor = ControlPaint.Dark(ColorResultCell, 0.1f);
            return;
        }

        Color bg = item switch
        {
            { HasErrors: true } => ColorError,
            { HasWarnings: true } => ColorWarning,
            { IsAutoFixed: true } => ColorAutoFixed,
            _ => ColorValid,
        };

        e.CellStyle.BackColor = bg;
        e.CellStyle.SelectionBackColor = ControlPaint.Dark(bg, 0.1f);
    }

    // --------------------------------------------------------------------------
    //  Grid refresh
    // --------------------------------------------------------------------------

    private void RefreshPatientGrid()
    {
        IEnumerable<PatientRowItem> items = _patientRows.Select(r => new PatientRowItem(r));
        if (_chkPatientFilterInvalid.Checked)
            items = items.Where(i => i.HasErrors || i.HasWarnings);

        _patientItems = new BindingList<PatientRowItem>(items.ToList());
        _patientGrid.DataSource = _patientItems;
        UpdatePatientStats();
    }

    private void RefreshTreatmentGrid()
    {
        IEnumerable<TreatmentRowItem> items = _treatmentRows.Select(r => new TreatmentRowItem(r));
        if (_chkTreatmentFilterInvalid.Checked)
            items = items.Where(i => i.HasErrors || i.HasWarnings);

        _treatmentItems = new BindingList<TreatmentRowItem>(items.ToList());
        _treatmentGrid.DataSource = _treatmentItems;
        UpdateTreatmentStats();
    }

    private void UpdatePatientStats()
    {
        int total = _patientRows.Count;
        int valid = _patientRows.Count(r => r.IsValid);
        int invalid = total - valid;

        (_lblPatientStats.Text, _lblPatientStats.ForeColor) = total switch
        {
            0 when _patientFileLoaded => ("✔ All patient rows processed.", Color.DarkGreen),
            0 => ("No file loaded.", Color.DimGray),
            _ when invalid == 0 => ($"Total: {total}   ✔ All valid", Color.DarkGreen),
            _ => ($"Total: {total}   ✔ Valid: {valid}   ✖ Needs attention: {invalid}", Color.DarkRed),
        };
    }

    private void UpdateTreatmentStats()
    {
        int total = _treatmentRows.Count;
        int valid = _treatmentRows.Count(r => r.IsValid);
        int invalid = total - valid;

        (_lblTreatmentStats.Text, _lblTreatmentStats.ForeColor) = total switch
        {
            0 when _treatmentFileLoaded => ("✔ All treatment rows processed.", Color.DarkGreen),
            0 => ("No file loaded.", Color.DimGray),
            _ when invalid == 0 => ($"Total: {total}   ✔ All valid", Color.DarkGreen),
            _ => ($"Total: {total}   ✔ Valid: {valid}   ✖ Needs attention: {invalid}", Color.DarkRed),
        };
    }

    // --------------------------------------------------------------------------
    //  Ingest
    // --------------------------------------------------------------------------

    private async Task IngestAsync()
    {
        if (!int.TryParse(_txtClinicId.Text.Trim(), out int clinicId) || clinicId <= 0)
        {
            MessageBox.Show("Please enter a valid numeric Clinic ID (must be ≥ 1).",
                "Invalid Clinic ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int validPatients = _patientRows.Count(r => r.IsValid);
        int validTreatments = _treatmentRows.Count(r => r.IsValid);

        if (validPatients == 0 && validTreatments == 0)
        {
            MessageBox.Show("There are no valid rows to ingest.\n\nRun Validate (and optionally Auto-Fix) first.",
                "Nothing to Ingest", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int skippedPatients = _patientRows.Count - validPatients;
        int skippedTreatments = _treatmentRows.Count - validTreatments;

        string previewMsg =
            $"Ready to ingest into Clinic ID {clinicId}:\n\n" +
            $"  Patients   — {validPatients} valid, {skippedPatients} will be skipped\n" +
            $"  Treatments — {validTreatments} valid, {skippedTreatments} will be skipped\n\n" +
            "Proceed?";

        if (MessageBox.Show(previewMsg, "Confirm Ingestion",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        SetStatus("Ingesting data — please wait…");
        _btnIngest.Enabled = false;
        Cursor = Cursors.WaitCursor;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ingestionSvc = scope.ServiceProvider.GetRequiredService<IIngestionService>();
            var report = await ingestionSvc.IngestAsync(clinicId, _patientRows, _treatmentRows);

            SetStatus($"Ingestion complete — {report.SuccessCount} inserted, " +
                      $"{report.SkippedCount} skipped, {report.ErrorCount} failed.");

            using var reportForm = new IngestionReportForm(report);
            reportForm.ShowDialog(this);

            // Post-ingestion cleanup:
            // Drop every row that was successfully persisted (Inserted or Duplicate).
            // What remains are only the invalid/skipped rows that still need attention.
            var ingestedPatientIds = report.Entries
                .Where(e => e.Status is IngestionStatus.Inserted or IngestionStatus.Duplicate)
                .Select(e => e.RawSourceId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Treatment RawSourceIds won't appear in the patient set, so we split
            // by checking whether each patient row's id appears in the report.
            _patientRows.RemoveAll(r => ingestedPatientIds.Contains(r.Row.RawSourceId));
            _treatmentRows.RemoveAll(r => r.IsValid);

            // Re-run cross-validation on remaining treatment rows whose patient
            // references may now resolve differently after the patient set shrank.
            RunTreatmentValidation();

            // Clear undo history — the ingested rows are gone; snapshots are stale.
            _patientUndoRedo.Clear();
            _treatmentUndoRedo.Clear();

            RefreshPatientGrid();
            RefreshTreatmentGrid();

            int remaining = _patientRows.Count + _treatmentRows.Count;
            SetStatus(remaining == 0
                ? $"Ingestion complete — all rows processed. ({report.SuccessCount} inserted, {report.DuplicateCount} duplicate)"
                : $"Ingestion complete — {report.SuccessCount} inserted. {remaining} row(s) still need attention.");
        }
        catch (Exception ex) { ShowError("Ingestion failed", ex); }
        finally
        {
            Cursor = Cursors.Default;
            UpdateButtons();
        }
    }

    // --------------------------------------------------------------------------
    //  Delete selected rows
    // --------------------------------------------------------------------------

    private void DeleteSelectedRows()
    {
        bool isPatient = _tabControl.SelectedTab == _patientTab;
        DataGridView grid = isPatient ? _patientGrid : _treatmentGrid;

        var selectedIndices = grid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => r.Index)
            .ToList();

        if (selectedIndices.Count == 0) return;

        string noun = isPatient ? "patient" : "treatment";
        string prompt = selectedIndices.Count == 1
            ? $"Delete the selected {noun} row?"
            : $"Delete {selectedIndices.Count} selected {noun} rows?";

        if (MessageBox.Show(prompt, "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        if (isPatient)
        {
            var toRemove = selectedIndices
                .Where(i => i >= 0 && i < _patientItems.Count)
                .Select(i => _patientItems[i].ParsedRow)
                .ToList();

            foreach (var row in toRemove)
                _patientRows.Remove(row);

            _patientUndoRedo.Clear();

            RunTreatmentValidation();
            MarkValidated();
            RefreshPatientGrid();
            RefreshTreatmentGrid();
        }
        else
        {
            var toRemove = selectedIndices
                .Where(i => i >= 0 && i < _treatmentItems.Count)
                .Select(i => _treatmentItems[i].ParsedRow)
                .ToList();

            foreach (var row in toRemove)
                _treatmentRows.Remove(row);

            _treatmentUndoRedo.Clear();
            MarkValidated();

            RefreshTreatmentGrid();
        }

        UpdateButtons();
        SetStatus($"Deleted {selectedIndices.Count} {noun} row(s).");
    }

    // --------------------------------------------------------------------------
    //  Export invalid rows
    // --------------------------------------------------------------------------

    private void ExportInvalid()
    {
        bool isPatient = _tabControl.SelectedTab == _patientTab;
        int invalidCnt = isPatient
            ? _patientRows.Count(r => !r.IsValid)
            : _treatmentRows.Count(r => !r.IsValid);

        if (invalidCnt == 0)
        {
            MessageBox.Show("There are no invalid rows to export on the current tab.",
                "Nothing to Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Title = "Export invalid rows",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = isPatient ? "patients_invalid.csv" : "treatments_invalid.csv",
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            int written = isPatient
                ? _exportService.ExportInvalid(_patientRows, dlg.FileName)
                : _exportService.ExportInvalid(_treatmentRows, dlg.FileName);

            SetStatus($"Exported {written} invalid row(s) to \"{Path.GetFileName(dlg.FileName)}\".");
            MessageBox.Show($"Exported {written} invalid row(s) to:\n{dlg.FileName}",
                "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError("Export failed", ex); }
    }

    // --------------------------------------------------------------------------
    //  Button state management
    // --------------------------------------------------------------------------

    private void UpdateButtons()
    {
        bool hasPatients = _patientRows.Count > 0;
        bool hasTreatments = _treatmentRows.Count > 0;
        bool hasAny = hasPatients || hasTreatments;
        bool hasInvalid = _patientRows.Any(r => !r.IsValid) || _treatmentRows.Any(r => !r.IsValid);
        bool isPatientTab = _tabControl.SelectedTab == _patientTab;

        _btnValidate.Enabled = hasAny && _validationDirty;
        _btnAutoFix.Enabled  = _autoFixReady;
        _btnIngest.Enabled   = hasAny;
        _btnExportInvalid.Enabled = hasInvalid;

        int selCount = isPatientTab
            ? _patientGrid.SelectedRows.Count
            : _treatmentGrid.SelectedRows.Count;

        _btnDeleteSelected.Enabled = selCount > 0;
        _btnDeleteSelected.Text = selCount switch
        {
            0 => "🗑 Delete Selected",
            1 => "🗑 Delete (1 row)",
            _ => $"🗑 Delete ({selCount} rows)",
        };

        _btnUndo.Enabled = isPatientTab
            ? _patientUndoRedo.CanUndo
            : _treatmentUndoRedo.CanUndo;

        _btnRedo.Enabled = isPatientTab
            ? _patientUndoRedo.CanRedo
            : _treatmentUndoRedo.CanRedo;
    }

    // --------------------------------------------------------------------------
    //  Helpers
    // --------------------------------------------------------------------------

    private PatientRowItem? PatientItemAt(int i)
        => (i >= 0 && i < _patientItems.Count) ? _patientItems[i] : null;

    private TreatmentRowItem? TreatmentItemAt(int i)
        => (i >= 0 && i < _treatmentItems.Count) ? _treatmentItems[i] : null;

    /// <summary>
    /// Ensures every column is wide enough to display its full header text on a
    /// single line.  Uses <see cref="TextRenderer.MeasureText"/> so the grid does
    /// not need to be visible or laid out — no tab-switching required.
    /// The column's current <c>Width</c> is preserved when it already exceeds the
    /// measured header width, so hand-tuned minimums (e.g. the Fill error column)
    /// are never shrunk.
    /// </summary>
    private static void AutoSizeColumnsToHeader(DataGridView grid)
    {
        // Extra horizontal padding inside a header cell (border + sort glyph space).
        const int HeaderPadding = 18;

        Font headerFont = grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font;

        foreach (DataGridViewColumn col in grid.Columns)
        {
            // Skip Fill columns — they manage their own width.
            if (col.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill) continue;

            int textWidth = TextRenderer.MeasureText(col.HeaderText, headerFont).Width;
            int requiredWidth = textWidth + HeaderPadding;

            col.MinimumWidth = requiredWidth;

            if (col.Width < requiredWidth)
                col.Width = requiredWidth;
        }
    }

    private void SetStatus(string message) => _statusLabel.Text = message;

    /// <summary>
    /// Marks data as changed since the last validation run, without any inline
    /// validation having run.  Enables Validate and disables AutoFix — the user
    /// must click Validate before Auto-Fix becomes meaningful again.
    /// </summary>
    private void MarkDirty()
    {
        _validationDirty = true;
        _autoFixReady    = false;
    }

    /// <summary>
    /// Records that the user changed data AND inline validation has already re-run
    /// (cell edit, undo, redo).  Keeps Validate enabled so the user can still
    /// trigger a full explicit re-validation, and re-enables Auto-Fix when errors
    /// remain so both options are available without forcing an extra click.
    /// </summary>
    private void MarkDataChanged()
    {
        _validationDirty = true;
        _autoFixReady    = _patientRows.Any(r => !r.IsValid) || _treatmentRows.Any(r => !r.IsValid);
    }

    /// <summary>
    /// Records that a full validation pass has just been run (explicit Validate,
    /// file load, or row deletion).  Disables Validate (nothing new to check) and
    /// enables Auto-Fix only when invalid rows remain.
    /// </summary>
    private void MarkValidated()
    {
        _validationDirty = false;
        _autoFixReady    = _patientRows.Any(r => !r.IsValid) || _treatmentRows.Any(r => !r.IsValid);
    }

    private void ShowError(string context, Exception ex)
    {
        SetStatus($"Error: {ex.Message}");
        MessageBox.Show($"{context}:\n\n{ex.Message}", "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
