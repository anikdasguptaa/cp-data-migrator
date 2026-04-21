namespace CP.Migrator.UI.Forms;

partial class IngestionReportForm
{
    private System.ComponentModel.IContainer components = null!;

    private Label _lblTotal;
    private Label _lblInserted;
    private Label _lblSkipped;
    private Label _lblDuplicate;
    private Label _lblFailed;
    private DataGridView _detailGrid;
    private Button _btnExportReport;
    private Button _btnClose;
    private Panel _summaryPanel;
    private Panel _buttonPanel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // Summary panel (top)
        _lblTotal = SummaryLabel();
        _lblInserted = SummaryLabel();
        _lblSkipped = SummaryLabel();
        _lblDuplicate = SummaryLabel();
        _lblFailed = SummaryLabel();

        _summaryPanel = new Panel { Dock = DockStyle.Top, Height = 140, Padding = new Padding(12, 10, 12, 6) };
        int y = 10;
        foreach (var lbl in new[] { _lblTotal, _lblInserted, _lblSkipped, _lblDuplicate, _lblFailed })
        {
            lbl.Location = new Point(12, y);
            y += 24;
            _summaryPanel.Controls.Add(lbl);
        }

        // Detail grid (centre)
        _detailGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None,
        };

        // Button panel (bottom)
        _btnExportReport = new Button
        {
            Text = "📤 Export Report",
            Width = 150,
            Height = 32,
            Location = new Point(12, 8),
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
        };
        _btnExportReport.Click += BtnExportReport_Click;

        _btnClose = new Button
        {
            Text = "Close",
            Width = 90,
            Height = 32,
            DialogResult = DialogResult.OK,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
        };

        _buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 52 };
        _buttonPanel.Controls.Add(_btnExportReport);
        _buttonPanel.Controls.Add(_btnClose);

        // Form
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(820, 520);
        MinimumSize = new Size(640, 400);
        Font = new Font("Segoe UI", 10F);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Ingestion Report";
        AcceptButton = _btnClose;

        Controls.Add(_detailGrid);
        Controls.Add(_summaryPanel);
        Controls.Add(_buttonPanel);

        ResumeLayout(false);

        // Position close button after layout so we can use ClientSize
        _btnClose.Location = new Point(ClientSize.Width - _btnClose.Width - 12, 8);
    }

    /// <summary>Creates a consistently styled <see cref="Label"/> for the summary panel.</summary>
    private static Label SummaryLabel() => new Label
    {
        AutoSize = true,
        Font = new Font("Segoe UI", 10F),
    };
}
