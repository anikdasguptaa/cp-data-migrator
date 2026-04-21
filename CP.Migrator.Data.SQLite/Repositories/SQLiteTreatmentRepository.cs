using CP.Migrator.Data.Repositories;
using CP.Migrator.Models.Entities;
using Dapper;

namespace CP.Migrator.Data.SQLite.Repositories;

/// <summary>
/// SQLite/Dapper implementation of <see cref="ITreatmentRepository"/>.
/// </summary>
internal class SQLiteTreatmentRepository : ITreatmentRepository
{
    private readonly IUnitOfWork _uow;

    public SQLiteTreatmentRepository(IUnitOfWork uow) => _uow = uow;

    public async Task<int> InsertAsync(Treatment entity)
    {
        const string sql = """
            INSERT INTO tblTreatment (
                TreatmentIdentifier, CompleteDate, Description, ItemCode, Tooth,
                Surface, Quantity, Fee, InvoiceId, PatientId, ClinicId, IsPaid, IsVoided)
            VALUES (
                @TreatmentIdentifier, @CompleteDate, @Description, @ItemCode, @Tooth,
                @Surface, @Quantity, @Fee, @InvoiceId, @PatientId, @ClinicId, @IsPaid, @IsVoided);
            SELECT last_insert_rowid();
            """;

        return await _uow.Connection.ExecuteScalarAsync<int>(sql, entity, _uow.Transaction);
    }
}
