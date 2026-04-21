namespace CP.Migrator.Models.Csv
{
	/// <summary>
	/// Represents a single row from patient.csv exactly as it appears in the file.
	/// Inherits RowIndex and RawSourceId from CsvRow — the Id column maps to RawSourceId.
	/// </summary>
	public class PatientCsvRow : CsvRow
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string DOB { get; set; }
		public string Gender { get; set; }
		public string Email { get; set; }
		public string MobileNumber { get; set; }
		public string PhoneNumber { get; set; }
		public string Street { get; set; }
		public string Suburb { get; set; }
		public string State { get; set; }
		public string Postcode { get; set; }
	}
}
