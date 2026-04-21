namespace CP.Migrator.Models.Csv
{
	/// <summary>
	/// Represents a single row from treatment.csv exactly as it appears in the file.
	/// Inherits RowIndex and RawSourceId from CsvRow — the Id column maps to RawSourceId.
	/// PatientID maps to RawPatientSourceId for cross-referencing against the patient import set.
	/// </summary>
	public class TreatmentCsvRow : CsvRow
	{
		/// <summary>
		/// Raw value from the PatientID column. Stored as a string for the same reason as RawSourceId —
		/// cross-database IDs are not guaranteed to be numeric or unique.
		/// </summary>
		public string RawPatientSourceId { get; set; }
		public string DentistId { get; set; }
		public string TreatmentItem { get; set; }
		public string Description { get; set; }
		public string Price { get; set; }
		public string Fee { get; set; }
		public string Date { get; set; }
		public string Paid { get; set; }
		public string ToothNumber { get; set; }
		public string Surface { get; set; }
	}
}
