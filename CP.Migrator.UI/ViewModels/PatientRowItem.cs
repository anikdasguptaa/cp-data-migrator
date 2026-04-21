using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CP.Migrator.UI.ViewModels;

/// <summary>
/// Bindable wrapper around <see cref="ParsedRow{PatientCsvRow}"/> for the DataGridView.
/// Property setters propagate changes back to the underlying row so the business
/// pipeline always works with the latest in-memory values.
/// </summary>
internal sealed class PatientRowItem : IRowItem, INotifyPropertyChanged
{
    /// <summary>The underlying parsed row, including the raw data and any validation errors.</summary>
    public ParsedRow<PatientCsvRow> ParsedRow { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PatientRowItem(ParsedRow<PatientCsvRow> parsedRow) => ParsedRow = parsedRow;

    // Read-only identity columns
    public int RowIndex => ParsedRow.Row.RowIndex;
    public string RawSourceId => ParsedRow.Row.RawSourceId;

    // Editable data columns — delegate to the underlying row
    public string FirstName
    {
        get => ParsedRow.Row.FirstName;
        set { ParsedRow.Row.FirstName = value; Notify(); }
    }

    public string LastName
    {
        get => ParsedRow.Row.LastName;
        set { ParsedRow.Row.LastName = value; Notify(); }
    }

    public string DOB
    {
        get => ParsedRow.Row.DOB;
        set { ParsedRow.Row.DOB = value; Notify(); }
    }

    public string Gender
    {
        get => ParsedRow.Row.Gender;
        set { ParsedRow.Row.Gender = value; Notify(); }
    }

    public string Email
    {
        get => ParsedRow.Row.Email;
        set { ParsedRow.Row.Email = value; Notify(); }
    }

    public string MobileNumber
    {
        get => ParsedRow.Row.MobileNumber;
        set { ParsedRow.Row.MobileNumber = value; Notify(); }
    }

    public string PhoneNumber
    {
        get => ParsedRow.Row.PhoneNumber;
        set { ParsedRow.Row.PhoneNumber = value; Notify(); }
    }

    public string Street
    {
        get => ParsedRow.Row.Street;
        set { ParsedRow.Row.Street = value; Notify(); }
    }

    public string Suburb
    {
        get => ParsedRow.Row.Suburb;
        set { ParsedRow.Row.Suburb = value; Notify(); }
    }

    public string State
    {
        get => ParsedRow.Row.State;
        set { ParsedRow.Row.State = value; Notify(); }
    }

    public string Postcode
    {
        get => ParsedRow.Row.Postcode;
        set { ParsedRow.Row.Postcode = value; Notify(); }
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
