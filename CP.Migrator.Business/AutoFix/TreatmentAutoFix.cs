using CP.Migrator.Models.Csv;

namespace CP.Migrator.Business.AutoFix
{
	/// <summary>
	/// Applies common automatic corrections to a <see cref="TreatmentCsvRow"/> in-place.
	/// </summary>
	internal class TreatmentAutoFix : AutoFixBase, IAutoFixService<TreatmentCsvRow>
	{
		public bool TryFix(TreatmentCsvRow row)
		{
			bool changed = false;

			changed |= SetIfChanged(row.RawSourceId, Trim, v => row.RawSourceId = v);
			changed |= SetIfChanged(row.RawPatientSourceId, Trim, v => row.RawPatientSourceId = v);
			changed |= SetIfChanged(row.DentistId, Trim, v => row.DentistId = v);
			changed |= SetIfChanged(row.TreatmentItem, Trim, v => row.TreatmentItem = v);
			changed |= SetIfChanged(row.Description, Trim, v => row.Description = v);
			changed |= SetIfChanged(row.Price, Trim, v => row.Price = v);
			changed |= SetIfChanged(row.Fee, Trim, v => row.Fee = v);
			changed |= SetIfChanged(row.ToothNumber, Trim, v => row.ToothNumber = v);
			changed |= SetIfChanged(row.Surface, Trim, v => row.Surface = v);

			changed |= SetIfChanged(row.Date, NormalizeDate, v => row.Date = v);
			changed |= SetIfChanged(row.Paid, NormalizePaid, v => row.Paid = v);

			return changed;
		}

		// -----------------------------------------------------------------------
		// Transforms
		// -----------------------------------------------------------------------

		/// <summary>Normalises "yes"/"YES"/"no"/"NO" → "Yes"/"No".</summary>
		private static string? NormalizePaid(string? value)
		{
			if (string.IsNullOrWhiteSpace(value)) return value;
			return value.Trim().ToUpperInvariant() switch
			{
				"YES" => "Yes",
				"NO" => "No",
				_ => value.Trim()
			};
		}
	}
}
