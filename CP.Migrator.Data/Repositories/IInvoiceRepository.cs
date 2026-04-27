using CP.Migrator.Data.Entities;

namespace CP.Migrator.Data.Repositories;

/// <summary>
/// Repository for <see cref="Invoice"/> entities.
/// Provides invoice-specific data operations beyond the base insert contract.
/// </summary>
public interface IInvoiceRepository : IRepository<Invoice, int>
{
    /// <summary>
    /// Returns the current maximum InvoiceNo in the database, or 0 if no invoices exist.
    /// Uses the connection and (optional) transaction from the injected <see cref="IUnitOfWork"/>.
    /// </summary>
    Task<int> GetMaxInvoiceNoAsync(int clinicId);
}
