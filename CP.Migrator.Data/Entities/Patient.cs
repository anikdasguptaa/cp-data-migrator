namespace CP.Migrator.Data.Entities
{
    /// <summary>
    /// Represents a row in the tblPatient database table.
    /// Mapped from <see cref="CP.Migrator.Models.Csv.PatientCsvRow"/> during ingestion.
    /// The <see cref="PatientId"/> is database-generated (AUTOINCREMENT) — the original
    /// CSV identifier is preserved in <see cref="PatientNo"/> for reference only.
    /// </summary>
    public class Patient
    {
        public int PatientId { get; set; }
        public string PatientIdentifier { get; set; }
        public string PatientNo { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Middlename { get; set; }
        public string PreferredName { get; set; }
        public string DateOfBirth { get; set; } // MSSQL type: datetime — stored as string (ISO 8601 TEXT) in SQLite; would be DateTime? in a typed MSSQL model
        public string Title { get; set; }
        public string Sex { get; set; }
        public string Email { get; set; }
        public string HomePhone { get; set; }
        public string Mobile { get; set; }
        public string Occupation { get; set; }
        public string CompanyName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Suburb { get; set; }
        public string Postcode { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public int ClinicId { get; set; }
        public int IsDeleted { get; set; } // MSSQL type: bit — stored as INTEGER 0/1 in SQLite; would be bool in a typed MSSQL model
    }
}
