using CP.Migrator.Models.Csv;

namespace CP.Migrator.Business.AutoFix
{
	/// <summary>
	/// Attempts to automatically correct common data problems on a CSV row in-place.
	/// Examples: trimming whitespace, normalising phone numbers, standardising date formats.
	/// If any field was changed the method returns <c>true</c> so that the UI can mark
	/// the row as auto-fixed for user visibility.
	/// <para>
	/// <strong>Pipeline order:</strong> Validation runs first on load so the user sees
	/// the full set of errors. Auto-fix is a <em>user-triggered</em> action — the user
	/// reviews the validation errors, clicks Auto-fix, and validation is then re-run so
	/// the UI reflects what was resolved and what still needs manual attention.
	/// This is why some validation error messages suggest "Try auto-fix" — the message
	/// is directing the user to that action in the UI.
	/// </para>
	/// </summary>
	public interface IAutoFixService<TRow> where TRow : CsvRow
	{
		/// <summary>
		/// Mutates <paramref name="row"/> in-place and returns <c>true</c> if at least
		/// one field was modified.
		/// </summary>
		bool TryFix(TRow row);
	}
}
