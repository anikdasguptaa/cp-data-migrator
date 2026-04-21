namespace CP.Migrator.Business.Config
{
    /// <summary>
    /// Tunable validation rules for treatment records.
    /// Defaults match the current Core Practice import rules.
    /// Override individual properties via DI registration or configuration binding.
    /// </summary>
    public class TreatmentValidationOptions
    {
        /// <summary>Accepted values for the Paid field (case-insensitive match).</summary>
        public HashSet<string> ValidPaidValues { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "Yes", "No"
        };

        /// <summary>The expected date format after auto-fix has run.</summary>
        public string ExpectedDateFormat { get; set; } = "yyyy-MM-dd";
    }
}
