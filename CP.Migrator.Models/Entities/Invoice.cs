namespace CP.Migrator.Models.Entities
{
	/// <summary>
	/// Represents a row in the tblInvoice database table.
	/// Created during ingestion by grouping treatments that share the same patient and date.
	/// The <see cref="Total"/> is calculated from the sum of related treatment fees.
	/// </summary>
	public class Invoice
	{
		public int InvoiceId { get; set; }
		public string InvoiceIdentifier { get; set; }
		public int InvoiceNo { get; set; }
		public string InvoiceDate { get; set; } // MSSQL type: datetime — stored as string (ISO 8601 TEXT) in SQLite; would be DateTime? in a typed MSSQL model
		public string DueDate { get; set; } // MSSQL type: datetime — stored as string (ISO 8601 TEXT) in SQLite; would be DateTime? in a typed MSSQL model
		public string Note { get; set; }
		public decimal Total { get; set; } // MSSQL type: decimal(19,4) — SQLite column is REAL (IEEE 754 double); decimal here preserves precision in C# but values read from SQLite may have already lost precision
		public decimal? Paid { get; set; } // MSSQL type: decimal(19,4) — same precision caveat as Total
		public decimal? Discount { get; set; } // MSSQL type: decimal(19,4) — same precision caveat as Total
		public int PatientId { get; set; }
		public int ClinicId { get; set; }
		public int IsDeleted { get; set; } // MSSQL type: bit — stored as INTEGER 0/1 in SQLite; would be bool in a typed MSSQL model
	}
}
