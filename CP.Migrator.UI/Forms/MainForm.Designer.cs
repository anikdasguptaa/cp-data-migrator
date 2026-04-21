namespace CP.Migrator.UI.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    // ToolStrip
    private ToolStrip _toolStrip;
    private ToolStripButton _btnLoadPatient;
    private ToolStripButton _btnLoadTreatment;
    private ToolStripSeparator _sep1;
    private ToolStripButton _btnValidate;
    private ToolStripButton _btnAutoFix;
    private ToolStripSeparator _sep2;
    private ToolStripButton _btnUndo;
    private ToolStripButton _btnRedo;
    private ToolStripSeparator _sep3;
    private ToolStripLabel _lblClinicIdLabel;
    private ToolStripTextBox _txtClinicId;
    private ToolStripButton _btnIngest;
    private ToolStripSeparator _sep4;
    private ToolStripButton _btnExportInvalid;
    private ToolStripSeparator _sep5;
    private ToolStripButton _btnDeleteSelected;

    // Tab control
    private TabControl _tabControl;
    private TabPage _patientTab;
    private TabPage _treatmentTab;

    // Patient tab
    private Panel _patientTopPanel;
    private CheckBox _chkPatientFilterInvalid;
    private Label _lblPatientStats;
    private DataGridView _patientGrid;

    // Treatment tab
    private Panel _treatmentTopPanel;
    private CheckBox _chkTreatmentFilterInvalid;
    private Label _lblTreatmentStats;
    private DataGridView _treatmentGrid;

    // Status strip
    private StatusStrip _statusStrip;
    private ToolStripStatusLabel _statusLabel;
    private ToolStripStatusLabel _dbPathLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // ToolStrip
        _btnLoadPatient = new ToolStripButton("📂 Load Patient CSV") { ToolTipText = "Open patient.csv" };
        _btnLoadTreatment = new ToolStripButton("📂 Load Treatment CSV") { ToolTipText = "Open treatment.csv" };
        _sep1 = new ToolStripSeparator();
        _btnValidate = new ToolStripButton("✔ Validate All") { ToolTipText = "Validate all rows across both Patients and Treatments tabs" };
        _btnAutoFix = new ToolStripButton("🔧 Auto-Fix All") { ToolTipText = "Apply automatic corrections (trim, phone/date formats, gender codes) across both Patients and Treatments, then re-validate", Enabled = false };
        _sep2 = new ToolStripSeparator();
        _btnUndo = new ToolStripButton("↩ Undo") { ToolTipText = "Undo last manual edit on the active tab", Enabled = false };
        _btnRedo = new ToolStripButton("↪ Redo") { ToolTipText = "Redo last undone edit on the active tab", Enabled = false };
        _sep3 = new ToolStripSeparator();
        _lblClinicIdLabel = new ToolStripLabel("Clinic ID:");
        _txtClinicId = new ToolStripTextBox { Text = "1", Width = 50, ToolTipText = "Target clinic identifier for ingestion" };
        _btnIngest = new ToolStripButton("📥 Ingest All") { ToolTipText = "Ingest all valid patients AND treatments into the database (both tabs)", Enabled = false };
        _sep4 = new ToolStripSeparator();
        _btnExportInvalid = new ToolStripButton("📤 Export Invalid (Tab)") { ToolTipText = "Export invalid rows from the active tab only (Patients or Treatments) to CSV for external correction", Enabled = false };
        _sep5 = new ToolStripSeparator();
        _btnDeleteSelected = new ToolStripButton("🗑 Delete Selected") { ToolTipText = "Delete the selected rows from the active tab", Enabled = false };

        _toolStrip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
        _toolStrip.Items.AddRange(new ToolStripItem[]
        {
            _btnLoadPatient, _btnLoadTreatment, _sep1,
            _btnValidate, _btnAutoFix, _sep2,
            _btnUndo, _btnRedo, _sep3,
            _lblClinicIdLabel, _txtClinicId, _btnIngest, _sep4,
            _btnExportInvalid, _sep5,
            _btnDeleteSelected,
        });

        // Patient tab
        _chkPatientFilterInvalid = new CheckBox
        {
            Text = "Show invalid rows only",
            AutoSize = true,
            Location = new Point(6, 5),
        };

        _lblPatientStats = new Label
        {
            Text = "No file loaded.",
            AutoSize = true,
            Location = new Point(210, 7),
            ForeColor = Color.DimGray,
        };

        _patientTopPanel = new Panel { Height = 28, Dock = DockStyle.Top };
        _patientTopPanel.Controls.Add(_chkPatientFilterInvalid);
        _patientTopPanel.Controls.Add(_lblPatientStats);

        _patientGrid = BuildGrid();

        _patientTab = new TabPage("🧑‍⚕️  Patients");
        _patientTab.Controls.Add(_patientGrid);
        _patientTab.Controls.Add(_patientTopPanel);

        // Treatment tab
        _chkTreatmentFilterInvalid = new CheckBox
        {
            Text = "Show invalid rows only",
            AutoSize = true,
            Location = new Point(6, 5),
        };

        _lblTreatmentStats = new Label
        {
            Text = "No file loaded.",
            AutoSize = true,
            Location = new Point(210, 7),
            ForeColor = Color.DimGray,
        };

        _treatmentTopPanel = new Panel { Height = 28, Dock = DockStyle.Top };
        _treatmentTopPanel.Controls.Add(_chkTreatmentFilterInvalid);
        _treatmentTopPanel.Controls.Add(_lblTreatmentStats);

        _treatmentGrid = BuildGrid();

        _treatmentTab = new TabPage("🦷  Treatments");
        _treatmentTab.Controls.Add(_treatmentGrid);
        _treatmentTab.Controls.Add(_treatmentTopPanel);

        // Tab control
        _tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
        _tabControl.TabPages.Add(_patientTab);
        _tabControl.TabPages.Add(_treatmentTab);

        // Status strip
        _statusLabel = new ToolStripStatusLabel("Ready")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _dbPathLabel = new ToolStripStatusLabel(
            $"DB: {Path.Combine(AppContext.BaseDirectory, "CorePractice.db")}")
        {
            ForeColor = Color.DimGray,
        };

        _statusStrip = new StatusStrip();
        _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _dbPathLabel });

        // Form
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1440, 780);
        MinimumSize = new Size(960, 600);
        Font = new Font("Segoe UI", 10F);
        Text = "Core Practice Data Migrator";
        StartPosition = FormStartPosition.CenterScreen;
        Name = "MainForm";

        // Add in reverse DockStyle priority order: Fill last → gets remaining space
        Controls.Add(_tabControl);
        Controls.Add(_toolStrip);
        Controls.Add(_statusStrip);

        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>Creates a consistently configured DataGridView for both tabs.</summary>
    private static DataGridView BuildGrid() => new DataGridView
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        ColumnHeadersHeight = 28,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            WrapMode = DataGridViewTriState.False,
        },
        BackgroundColor = SystemColors.Window,
        BorderStyle = BorderStyle.None,
        ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
        RowTemplate = { Height = 24 },
    };
}
