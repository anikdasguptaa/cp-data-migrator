using CP.Migrator.Data.Entities;

namespace CP.Migrator.Data.Repositories;

/// <summary>
/// Repository for <see cref="InvoiceLineItem"/> entities.
/// One line item is inserted per treatment within an invoice during ingestion.
/// </summary>
public interface IInvoiceLineItemRepository : IRepository<InvoiceLineItem, int>
{
}
