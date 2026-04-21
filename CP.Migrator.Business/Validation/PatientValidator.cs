using CP.Migrator.Business.Config;
using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CP.Migrator.Business.Validation
{
	/// <summary>
	/// Validates a <see cref="PatientCsvRow"/> against Core Practice schema rules.
	/// Errors are appended directly to the <see cref="ParsedRow{T}"/> so they travel
	/// with the row through the rest of the pipeline and into the UI grid.
	/// Rules (states, genders, patterns, thresholds) are driven by
	/// <see cref="PatientValidationOptions"/> so they can be changed without code edits.
	/// </summary>
	internal class PatientValidator : IRecordValidator<PatientCsvRow>
	{
		private readonly PatientValidationOptions _options;
		private readonly Regex _emailRegex;
		private readonly Regex _mobileRegex;
		private readonly Regex _phoneRegex;
		private readonly Regex _postcodeRegex;

		// Cache the duplicate-ID lookup so repeated Validate() calls with the same
		// patient rows reference don't rebuild the lookup every time.
		private IEnumerable<ParsedRow<PatientCsvRow>>? _lastPatientRowsRef;
		private Dictionary<string, int> _sourceIdToFirstRow = new(StringComparer.OrdinalIgnoreCase);

		public PatientValidator() : this(new PatientValidationOptions()) { }

		public PatientValidator(PatientValidationOptions options)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));
			_emailRegex = new Regex(options.EmailPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
			_mobileRegex = new Regex(options.MobilePattern, RegexOptions.Compiled);
			_phoneRegex = new Regex(options.PhonePattern, RegexOptions.Compiled);
			_postcodeRegex = new Regex(options.PostcodePattern, RegexOptions.Compiled);
		}

		public void Validate(ParsedRow<PatientCsvRow> parsedRow, IEnumerable<ParsedRow<PatientCsvRow>> allRows)
		{
			var row = parsedRow.Row;

			// Rebuild the duplicate-ID lookup only when the patient rows reference changes
			if (!ReferenceEquals(allRows, _lastPatientRowsRef))
			{
				_lastPatientRowsRef = allRows;
				_sourceIdToFirstRow = allRows
					.Where(r => !string.IsNullOrWhiteSpace(r.Row.RawSourceId))
					.GroupBy(r => r.Row.RawSourceId, StringComparer.OrdinalIgnoreCase)
					.ToDictionary(g => g.Key, g => g.Min(r => r.Row.RowIndex), StringComparer.OrdinalIgnoreCase);
			}

			// --- Intra-CSV duplicate patient ID ---
			if (!string.IsNullOrWhiteSpace(row.RawSourceId)
				&& _sourceIdToFirstRow.TryGetValue(row.RawSourceId, out var firstRow)
				&& firstRow != row.RowIndex)
			{
				parsedRow.Errors.Add(new ValidationError("ID",
					$"Duplicate patient ID '{row.RawSourceId}' — already seen on row {firstRow}. Remove this row or change the ID."));
			}

			// --- FirstName (required, max 50) ---
			if (string.IsNullOrWhiteSpace(row.FirstName))
				parsedRow.Errors.Add(new ValidationError("FirstName", "First name is required."));
			else if (row.FirstName.Length > 50)
				parsedRow.Errors.Add(new ValidationError("FirstName",
					$"First name exceeds maximum length of 50 characters ({row.FirstName.Length} provided)."));

			// --- LastName (required, max 50) ---
			if (string.IsNullOrWhiteSpace(row.LastName))
				parsedRow.Errors.Add(new ValidationError("LastName", "Last name is required."));
			else if (row.LastName.Length > 50)
				parsedRow.Errors.Add(new ValidationError("LastName",
					$"Last name exceeds maximum length of 50 characters ({row.LastName.Length} provided)."));

			// --- Date of birth (optional) ---
			if (!string.IsNullOrWhiteSpace(row.DOB))
			{
				if (!DateTime.TryParseExact(row.DOB, _options.ExpectedDateFormat, CultureInfo.InvariantCulture,
						 DateTimeStyles.None, out var dob))
				{
					parsedRow.Errors.Add(new ValidationError("DOB",
						$"'{row.DOB}' is not a recognised date. Expected {_options.ExpectedDateFormat}. Try auto-fix."));
				}
				else
				{
					if (dob.Date > DateTime.Today)
						parsedRow.Errors.Add(new ValidationError("DOB", "Date of birth cannot be in the future."));
					else if (dob.Date < DateTime.Today.AddYears(-_options.MaxAgeYears))
						parsedRow.Errors.Add(new ValidationError("DOB",
							$"Date of birth is unrealistically old (more than {_options.MaxAgeYears} years ago)."));
				}
			}

			// --- Email (optional, max 256) ---
			if (!string.IsNullOrWhiteSpace(row.Email))
			{
				if (row.Email.Length > 256)
					parsedRow.Errors.Add(new ValidationError("Email",
						$"Email exceeds maximum length of 256 characters ({row.Email.Length} provided)."));
				else if (!_emailRegex.IsMatch(row.Email))
					parsedRow.Errors.Add(new ValidationError("Email",
						$"'{row.Email}' is not a valid email address."));
			}

			// --- MobileNumber (optional, max 25) ---
			if (!string.IsNullOrWhiteSpace(row.MobileNumber))
			{
				if (row.MobileNumber.Length > 25)
					parsedRow.Errors.Add(new ValidationError("MobileNumber",
						$"Mobile number exceeds maximum length of 25 characters ({row.MobileNumber.Length} provided)."));
				else if (!_mobileRegex.IsMatch(row.MobileNumber))
					parsedRow.Errors.Add(new ValidationError("MobileNumber",
						$"'{row.MobileNumber}' must be 10 digits starting with 04 (e.g. 0412345678). Try auto-fix."));
			}

			// --- PhoneNumber (optional, max 25) ---
			if (!string.IsNullOrWhiteSpace(row.PhoneNumber))
			{
				if (row.PhoneNumber.Length > 25)
					parsedRow.Errors.Add(new ValidationError("PhoneNumber",
						$"Phone number exceeds maximum length of 25 characters ({row.PhoneNumber.Length} provided)."));
				else if (!_phoneRegex.IsMatch(row.PhoneNumber))
					parsedRow.Errors.Add(new ValidationError("PhoneNumber",
						$"'{row.PhoneNumber}' must be 10 digits. Try auto-fix."));
			}

			// --- Street (optional, max 256) ---
			if (!string.IsNullOrWhiteSpace(row.Street) && row.Street.Length > 256)
				parsedRow.Errors.Add(new ValidationError("Street",
					$"Street exceeds maximum length of 256 characters ({row.Street.Length} provided)."));

			// --- Suburb (optional, max 256) ---
			if (!string.IsNullOrWhiteSpace(row.Suburb) && row.Suburb.Length > 256)
				parsedRow.Errors.Add(new ValidationError("Suburb",
					$"Suburb exceeds maximum length of 256 characters ({row.Suburb.Length} provided)."));

			// --- State (optional, max 6) ---
			if (!string.IsNullOrWhiteSpace(row.State))
			{
				if (row.State.Length > 6)
					parsedRow.Errors.Add(new ValidationError("State",
						$"State exceeds maximum length of 6 characters ({row.State.Length} provided)."));
				else if (!_options.ValidStates.Contains(row.State))
						parsedRow.Errors.Add(new ValidationError("State",
							$"'{row.State}' is not a valid Australian state or territory ({string.Join(", ", _options.ValidStates)})."));
			}

			// --- Postcode (optional, max 6) ---
			if (!string.IsNullOrWhiteSpace(row.Postcode))
			{
				if (row.Postcode.Length > 6)
					parsedRow.Errors.Add(new ValidationError("Postcode",
						$"Postcode exceeds maximum length of 6 characters ({row.Postcode.Length} provided)."));
				else if (!_postcodeRegex.IsMatch(row.Postcode))
					parsedRow.Errors.Add(new ValidationError("Postcode",
						$"'{row.Postcode}' must be a 4-digit postcode."));
			}

			// --- Gender (optional, max 2) ---
			if (!string.IsNullOrWhiteSpace(row.Gender))
			{
				if (!_options.ValidGenders.Contains(row.Gender))
					parsedRow.Errors.Add(new ValidationError("Gender",
						$"'{row.Gender}' is not a recognised gender value ({string.Join(", ", _options.ValidGenders)}). Try auto-fix."));
				else if (row.Gender.Length > 2)
					parsedRow.Errors.Add(new ValidationError("Gender",
						$"Gender exceeds maximum length of 2 characters ({row.Gender.Length} provided). Try auto-fix."));
			}
		}
	}
}

