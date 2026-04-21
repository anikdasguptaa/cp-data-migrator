namespace CP.Migrator.Business.Config
{
    /// <summary>
    /// Tunable validation rules for patient records.
    /// Defaults match the current Australian dental-practice rules.
    /// Override individual properties via DI registration or configuration binding.
    /// </summary>
    public class PatientValidationOptions
    {
        /// <summary>Valid Australian state/territory codes.</summary>
        public HashSet<string> ValidStates { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "NSW", "VIC", "QLD", "SA", "WA", "TAS", "NT", "ACT"
        };

        /// <summary>Accepted gender values (case-insensitive match).</summary>
        public HashSet<string> ValidGenders { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "M", "F", "O"
        };

        /// <summary>Regex pattern for email validation.</summary>
        public string EmailPattern { get; set; } = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        /// <summary>Regex pattern for Australian mobile numbers (10 digits starting with 04).</summary>
        public string MobilePattern { get; set; } = @"^04\d{8}$";

        /// <summary>Regex pattern for Australian landline numbers (10 digits).</summary>
        public string PhonePattern { get; set; } = @"^\d{10}$";

        /// <summary>Regex pattern for Australian postcodes (4 digits).</summary>
        public string PostcodePattern { get; set; } = @"^\d{4}$";

        /// <summary>Expected date format for date-of-birth values.</summary>
        public string ExpectedDateFormat { get; set; } = "yyyy-MM-dd";

        /// <summary>Maximum patient age in years. DOB older than this is flagged as unrealistic.</summary>
        public int MaxAgeYears { get; set; } = 130;
    }
}
