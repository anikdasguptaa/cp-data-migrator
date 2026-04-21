using CP.Migrator.Models.Csv;
using System.Text.RegularExpressions;

namespace CP.Migrator.Business.AutoFix
{
    /// <summary>
    /// Applies common automatic corrections to a <see cref="PatientCsvRow"/> in-place.
    /// </summary>
    internal class PatientAutoFix : AutoFixBase, IAutoFixService<PatientCsvRow>
    {
		public bool TryFix(PatientCsvRow row)
        {
            bool changed = false;

            changed |= SetIfChanged(row.FirstName, Trim, v => row.FirstName = v);
            changed |= SetIfChanged(row.LastName, Trim, v => row.LastName = v);
            changed |= SetIfChanged(row.Gender, NormalizeGender, v => row.Gender = v);
            changed |= SetIfChanged(row.Email, Trim, v => row.Email = v);
            changed |= SetIfChanged(row.Street, Trim, v => row.Street = v);
            changed |= SetIfChanged(row.Suburb, Trim, v => row.Suburb = v);
            changed |= SetIfChanged(row.State, Trim, v => row.State = v);
            changed |= SetIfChanged(row.Postcode, Trim, v => row.Postcode = v);

            // Phone normalization: strip non-digits then trim (order matters)
            changed |= SetIfChanged(row.MobileNumber, NormalizePhone, v => row.MobileNumber = v);
            changed |= SetIfChanged(row.PhoneNumber, NormalizePhone, v => row.PhoneNumber = v);

            // Date: trim first, then reformat
            changed |= SetIfChanged(row.DOB, NormalizeDate, v => row.DOB = v);

            return changed;
        }

        // -----------------------------------------------------------------------
        // Transforms
        // -----------------------------------------------------------------------

        /// <summary>
        /// Strips all non-digit characters so "mum 0 4 0 1 8 7 8 0 3 4" → "0401878034".
        /// Also trims surrounding whitespace. Leaves unchanged if no digits remain.
        /// </summary>
        private static string? NormalizePhone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var digits = Regex.Replace(value, @"\D", "");
            return digits.Length > 0 ? digits : value.Trim();
        }

        /// <summary>Normalises "Male"/"male" → "M", "Female"/"female" → "F", "Other"/"other" → "O".</summary>
        private static string? NormalizeGender(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return value.Trim().ToUpperInvariant() switch
            {
                "MALE" => "M",
                "FEMALE" => "F",
                "OTHER" => "O",
                _ => value.Trim()
            };
        }
    }
}
