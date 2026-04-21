using CP.Migrator.Models.Results;

namespace CP.Migrator.UI.Forms;

/// <summary>
/// Modal dialog shown after a completed ingestion run.
/// Displays top-level totals and a per-row detail grid so the operator
/// can see exactly what was inserted, skipped, duplicated, or failed.
/// </summary>
internal partial class IngestionReportForm : Form
{
    private readonly IngestionReport _report;

    /// <summary>Initialises the report dialog with the results of a completed ingestion run.</summary>
    /// <param name="report">The report produced by <see cref="CP.Migrator.Business.Ingestion.IIngestionService"/>.</param>
    public IngestionReportForm(IngestionReport report)
    {
        _report = report;
        InitializeComponent();
        Populate();
    }

    /// <summary>
    /// Populates the summary labels and detail grid from <see cref="_report"/>.
    /// Colour-codes each detail row by its <see cref="IngestionStatus"/>.
    /// </summary>
    private void Populate()
    {
		_lblTotal.Text = $"Total processed: {_report.TotalRows}";
		_lblInserted.Text = $"✔ Inserted: {_report.SuccessCount}";
		_lblSkipped.Text = $"⊘ Skipped: {_report.SkippedCount}";
		_lblDuplicate.Text = $"⧉ Duplicates: {_report.DuplicateCount}";
		_lblFailed.Text = $"✖ Failed: {_report.ErrorCount}";

		_lblInserted.ForeColor = _report.SuccessCount > 0 ? Color.DarkGreen : Color.Black;
		_lblSkipped.ForeColor = _report.SkippedCount > 0 ? Color.DarkGoldenrod : Color.Black;
		_lblDuplicate.ForeColor = _report.DuplicateCount > 0 ? Color.DarkOrange : Color.Black;
		_lblFailed.ForeColor = _report.ErrorCount > 0 ? Color.DarkRed : Color.Black;

        _detailGrid.DataSource = _report.Entries
            .Select(e => new
            {
                Id = e.RawSourceId,
                Status = e.Status.ToString(),
                e.Message,
            })
            .ToList();

        if (_detailGrid.Columns.Count > 0)
        {
            _detailGrid.Columns["Id"]!.Width = 90;
            _detailGrid.Columns["Status"]!.Width = 100;
            _detailGrid.Columns["Message"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        // Colour-code status cells
        foreach (DataGridViewRow row in _detailGrid.Rows)
        {
            var status = row.Cells["Status"]?.Value?.ToString();
            row.DefaultCellStyle.BackColor = status switch
            {
                "Inserted" => Color.FromArgb(220, 255, 220),
                "Skipped" => Color.FromArgb(255, 245, 200),
                "Failed" => Color.FromArgb(255, 220, 220),
                "Duplicate" => Color.FromArgb(220, 235, 255),
                _ => Color.White,
            };
        }
    }

    /// <summary>Prompts for a save location and writes all report entries to a CSV file.</summary>
    private void BtnExportReport_Click(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Save ingestion report",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = $"ingestion_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            var lines = new List<string> { "Id,Status,Message" };
            lines.AddRange(_report.Entries.Select(e =>
                $"{Escape(e.RawSourceId)},{e.Status},{Escape(e.Message)}"));

            File.WriteAllLines(dlg.FileName, lines);
            MessageBox.Show($"Report saved to {Path.GetFileName(dlg.FileName)}.",
                "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed:\n\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// RFC 4180-escapes a value for safe inclusion in a CSV field.
    /// Wraps the value in double quotes and escapes embedded double quotes when
    /// the value contains a comma, a double quote, or a newline character.
    /// </summary>
    /// <param name="value">The raw string to escape. May be <c>null</c>.</param>
    /// <returns>
    /// The escaped string, or <see cref="string.Empty"/> when <paramref name="value"/> is
    /// <c>null</c> or empty.
    /// </returns>
    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        value = value.Replace("\"", "\"\"");
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value}\""
            : value;
    }
}
