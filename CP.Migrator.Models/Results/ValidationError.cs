namespace CP.Migrator.Models.Results
{
    public enum ValidationSeverity
    {
        /// <summary>Row can still be ingested but the value was unusual (e.g. empty optional field).</summary>
        Warning,

        /// <summary>Row must not be ingested until this error is resolved.</summary>
        Error
    }

    /// <summary>
    /// Describes a single validation problem on a specific field of a CSV row.
    /// </summary>
    public class ValidationError
    {
        public string FieldName { get; set; }
        public string Message { get; set; }
        public ValidationSeverity Severity { get; set; }

        public ValidationError(string fieldName, string message, ValidationSeverity severity = ValidationSeverity.Error)
        {
            FieldName = fieldName;
            Message = message;
            Severity = severity;
        }

        public override string ToString() => $"[{Severity}] {FieldName}: {Message}";
    }
}
