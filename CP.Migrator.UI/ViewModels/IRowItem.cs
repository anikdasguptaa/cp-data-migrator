namespace CP.Migrator.UI.ViewModels;

/// <summary>
/// Common interface for grid row view-models so the shared row-colouring
/// event handler can work across both the patient and treatment grids
/// without needing to know the concrete type.
/// </summary>
internal interface IRowItem
{
    /// <summary>Returns <see langword="true"/> when the row has at least one error-severity validation result.</summary>
    bool HasErrors { get; }

    /// <summary>Returns <see langword="true"/> when the row has no errors but at least one warning-severity validation result.</summary>
    bool HasWarnings { get; }

    /// <summary>Returns <see langword="true"/> when an auto-fix pass modified the row's data.</summary>
    bool IsAutoFixed { get; }
}
