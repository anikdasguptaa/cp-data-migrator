using System.Globalization;
using System.Text.RegularExpressions;

namespace CP.Migrator.Business.AutoFix
{
	/// <summary>
	/// Base class for auto-fix services. Provides shared helper methods for
	/// applying field-level transforms (trimming, date normalisation) and
	/// tracking whether any value was actually changed.
	/// </summary>
	internal abstract class AutoFixBase
	{
		protected static readonly string[] DateFormats =
		[
			"yyyy-MM-dd", "dd/MM/yyyy", "d/MM/yyyy", "MM/dd/yyyy",
			"dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy"
		];

		// -----------------------------------------------------------------------
		// Helper — applies a transform, assigns back, returns whether it changed
		// -----------------------------------------------------------------------

		protected static bool SetIfChanged(string? current, Func<string?, string?> transform, Action<string?> assign)
		{
			var next = transform(current);
			if (next == current) return false;
			assign(next);
			return true;
		}

		// -----------------------------------------------------------------------
		// Shared transforms
		// -----------------------------------------------------------------------

		protected static string? Trim(string? value) => value?.Trim();

		/// <summary>
		/// Parses several common date formats and re-formats as yyyy-MM-dd.
		/// Returns unchanged if already correct or unrecognised.
		/// </summary>
		protected static string? NormalizeDate(string? value)
		{
			if (string.IsNullOrWhiteSpace(value)) return value;
			var trimmed = value.Trim();
			if (Regex.IsMatch(trimmed, @"^\d{4}-\d{2}-\d{2}$")) return trimmed;

			if (DateTime.TryParseExact(trimmed, DateFormats, CultureInfo.InvariantCulture,
					DateTimeStyles.None, out var parsed))
				return parsed.ToString("yyyy-MM-dd");

			return trimmed;
		}
	}
}
