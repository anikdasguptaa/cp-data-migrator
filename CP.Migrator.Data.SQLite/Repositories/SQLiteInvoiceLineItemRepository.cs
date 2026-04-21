using CP.Migrator.Data.Repositories;
using CP.Migrator.Models.Entities;
using Dapper;

namespace CP.Migrator.Data.SQLite.Repositories;

/// <summary>
/// SQLite/Dapper implementation of <see cref="IInvoiceLineItemRepository"/>.
/// </summary>
internal class SQLiteInvoiceLineItemRepository : IInvoiceLineItemRepository
{
    private readonly IUnitOfWork _uow;

    public SQLiteInvoiceLineItemRepository(IUnitOfWork uow) => _uow = uow;

    public async Task<int> InsertAsync(InvoiceLineItem entity)
    {
        const string sql = """
            INSERT INTO tblInvoiceLineItem (
                InvoiceLineItemIdentifier, Description, ItemCode, Quantity,
                UnitAmount, LineAmount, PatientId, TreatmentId, InvoiceId, ClinicId)
            VALUES (
                @InvoiceLineItemIdentifier, @Description, @ItemCode, @Quantity,
                @UnitAmount, @LineAmount, @PatientId, @TreatmentId, @InvoiceId, @ClinicId);
            SELECT last_insert_rowid();
            """;

        return await _uow.Connection.ExecuteScalarAsync<int>(sql, entity, _uow.Transaction);
    }
}
