using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CP.Migrator.UI.ViewModels;

/// <summary>
/// Bindable wrapper around <see cref="ParsedRow{TreatmentCsvRow}"/> for the DataGridView.
/// Property setters propagate changes back to the underlying row so the business
/// pipeline always works with the latest in-memory values.
/// </summary>
internal sealed class TreatmentRowItem : IRowItem, INotifyPropertyChanged
{
    /// <summary>The underlying parsed row, including the raw data and any validation errors.</summary>
    public ParsedRow<TreatmentCsvRow> ParsedRow { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TreatmentRowItem(ParsedRow<TreatmentCsvRow> parsedRow) => ParsedRow = parsedRow;

    // Read-only identity columns
    public int RowIndex => ParsedRow.Row.RowIndex;
    public string RawSourceId => ParsedRow.Row.RawSourceId;
    public string RawPatientSourceId => ParsedRow.Row.RawPatientSourceId;

    // Editable data columns — delegate to the underlying row
    public string DentistId
    {
        get => ParsedRow.Row.DentistId;
        set { ParsedRow.Row.DentistId = value; Notify(); }
    }

    public string TreatmentItem
    {
        get => ParsedRow.Row.TreatmentItem;
        set { ParsedRow.Row.TreatmentItem = value; Notify(); }
    }

    public string Description
    {
        get => ParsedRow.Row.Description;
        set { ParsedRow.Row.Description = value; Notify(); }
    }

    public string Price
    {
        get => ParsedRow.Row.Price;
        set { ParsedRow.Row.Price = value; Notify(); }
    }

    public string Fee
    {
        get => ParsedRow.Row.Fee;
        set { ParsedRow.Row.Fee = value; Notify(); }
    }

    public string Date
    {
        get => ParsedRow.Row.Date;
        set { ParsedRow.Row.Date = value; Notify(); }
    }

    public string Paid
    {
        get => ParsedRow.Row.Paid;
        set { ParsedRow.Row.Paid = value; Notify(); }
    }

    public string ToothNumber
    {
        get => ParsedRow.Row.ToothNumber;
        set { ParsedRow.Row.ToothNumber = value; Notify(); }
    }

    public string Surface
    {
        get => ParsedRow.Row.Surface;
        set { ParsedRow.Row.Surface = value; Notify(); }
    }

    // Derived display columns
    public bool HasErrors => ParsedRow.Errors.Any(e => e.Severity == ValidationSeverity.Error);
    public bool HasWarnings => !HasErrors && ParsedRow.Errors.Any(e => e.Severity == ValidationSeverity.Warning);
    public bool IsAutoFixed => ParsedRow.IsAutoFixed;

    /// <summary>Short status label shown in the grid Status column (e.g. "✔ Valid", "✖ Invalid").</summary>
    public string StatusBadge =>
        HasErrors ? "✖ Invalid" :
        HasWarnings ? "⚠ Warning" :
        IsAutoFixed ? "✔ Fixed" :
                      "✔ Valid";

    /// <summary>Pipe-separated list of all validation messages for the row, shown in the grid Errors column.</summary>
    public string ErrorSummary =>
        string.Join(" | ", ParsedRow.Errors.Select(e => e.ToString()));

    private void Notify([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
