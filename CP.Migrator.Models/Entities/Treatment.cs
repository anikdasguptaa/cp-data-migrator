namespace CP.Migrator.Models.Entities
{
	/// <summary>
	/// Represents a row in the tblTreatment database table.
	/// Mapped from <see cref="CP.Migrator.Models.Csv.TreatmentCsvRow"/> during ingestion.
	/// Each treatment is linked to a <see cref="Patient"/> and optionally to an <see cref="Invoice"/>.
	/// </summary>
	public class Treatment
	{
		public int TreatmentId { get; set; }
		public string TreatmentIdentifier { get; set; }
		public string CompleteDate { get; set; } // MSSQL type: datetime — stored as string (ISO 8601 TEXT) in SQLite; would be DateTime? in a typed MSSQL model
		public string Description { get; set; }
		public string ItemCode { get; set; }
		public string Tooth { get; set; }
		public string Surface { get; set; }
		public int Quantity { get; set; }
		public decimal Fee { get; set; } // MSSQL type: decimal(19,4) — SQLite column is REAL (IEEE 754 double); decimal here preserves precision in C# but values read from SQLite may have already lost precision
		public int? InvoiceId { get; set; }
		public int PatientId { get; set; }
		public int ClinicId { get; set; }
		public int IsPaid { get; set; } // MSSQL type: bit — stored as INTEGER 0/1 in SQLite; would be bool in a typed MSSQL model
		public int IsVoided { get; set; } // MSSQL type: bit — stored as INTEGER 0/1 in SQLite; would be bool in a typed MSSQL model
	}
}
