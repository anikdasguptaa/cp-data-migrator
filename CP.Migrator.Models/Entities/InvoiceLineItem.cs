namespace CP.Migrator.Models.Entities
{
	/// <summary>
	/// Represents a row in the tblInvoiceLineItem database table.
	/// One line item is created per treatment within an invoice during ingestion.
	/// </summary>
	public class InvoiceLineItem
	{
		public int InvoiceLineItemId { get; set; }
		public string InvoiceLineItemIdentifier { get; set; }
		public string Description { get; set; }
		public string ItemCode { get; set; }
		public int Quantity { get; set; }
		public decimal UnitAmount { get; set; } // MSSQL type: decimal(19,4) — SQLite column is REAL (IEEE 754 double); decimal here preserves precision in C# but values read from SQLite may have already lost precision
		public decimal LineAmount { get; set; } // MSSQL type: decimal(19,4) — same precision caveat as UnitAmount
		public int PatientId { get; set; }
		public int TreatmentId { get; set; }
		public int InvoiceId { get; set; }
		public int ClinicId { get; set; }
	}
}
