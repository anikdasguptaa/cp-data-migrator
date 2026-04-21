using CP.Migrator.Data.Repositories;
using CP.Migrator.Models.Entities;
using Dapper;

namespace CP.Migrator.Data.SQLite.Repositories;

/// <summary>
/// SQLite/Dapper implementation of <see cref="IInvoiceRepository"/>.
/// </summary>
internal class SQLiteInvoiceRepository : IInvoiceRepository
{
    private readonly IUnitOfWork _uow;

    public SQLiteInvoiceRepository(IUnitOfWork uow) => _uow = uow;

    public async Task<int> InsertAsync(Invoice entity)
    {
        const string sql = """
            INSERT INTO tblInvoice (
                InvoiceIdentifier, InvoiceNo, InvoiceDate, DueDate, Note,
                Total, Paid, Discount, PatientId, ClinicId, IsDeleted)
            VALUES (
                @InvoiceIdentifier, @InvoiceNo, @InvoiceDate, @DueDate, @Note,
                @Total, @Paid, @Discount, @PatientId, @ClinicId, @IsDeleted);
            SELECT last_insert_rowid();
            """;

        return await _uow.Connection.ExecuteScalarAsync<int>(sql, entity, _uow.Transaction);
    }

    public async Task<int> GetMaxInvoiceNoAsync(int clinicId)
    {
        return await _uow.Connection.ExecuteScalarAsync<int>(
            "SELECT COALESCE(MAX(InvoiceNo), 0) FROM tblInvoice WHERE ClinicId = @clinicId;",
            new { clinicId },
            transaction: _uow.Transaction);
    }
}
