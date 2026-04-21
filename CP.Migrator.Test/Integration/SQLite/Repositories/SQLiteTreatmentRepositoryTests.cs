using CP.Migrator.Data;
using CP.Migrator.Data.SQLite;
using CP.Migrator.Data.SQLite.Repositories;
using CP.Migrator.Models.Entities;
using CP.Migrator.Test.Shared;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace CP.Migrator.Test.Integration.SQLite.Repositories;

public class SQLiteTreatmentRepositoryTests : IDisposable
{
    private readonly FixedConnectionFactory _factory = new();
    private readonly IDbConnection _keepAlive;
    private readonly IUnitOfWork _uow;
    private readonly SQLiteTreatmentRepository _repository;

    public SQLiteTreatmentRepositoryTests()
    {
        _keepAlive = _factory.CreateConnection();
        _keepAlive.Open();
        new SQLiteDatabaseInitializer(_factory).Initialise();
        _uow = new SQLiteUnitOfWork(_factory);
        _repository = new SQLiteTreatmentRepository(_uow);
    }

    public void Dispose()
    {
        _uow.Dispose();
        _keepAlive.Dispose();
    }

    private static void SeedPatient(IDbConnection connection, int patientId)
    {
        connection.Execute(
            $"INSERT INTO tblPatient (PatientId, PatientIdentifier, Firstname, Lastname, ClinicId, IsDeleted) VALUES ({patientId}, 'P{patientId}', 'Jane', 'Doe', 1, 0);");
    }

    private static Treatment BuildTreatment(int patientId, string identifier = "T001") => new()
    {
        TreatmentIdentifier = identifier,
        CompleteDate = "2024-01-10",
        Description = "Filling",
        ItemCode = "D2140",
        Tooth = "14",
        Surface = "O",
        Quantity = 1,
        Fee = 150.00m,
        InvoiceId = null,
        PatientId = patientId,
        ClinicId = 1,
        IsPaid = 0,
        IsVoided = 0
    };

    [Fact]
    public async Task InsertAsync_InsertsTreatmentRow()
    {
        SeedPatient(_keepAlive, 1);

        _uow.BeginTransaction();
        await _repository.InsertAsync(BuildTreatment(1));
        _uow.Commit();

        var count = _keepAlive.ExecuteScalar<int>("SELECT COUNT(*) FROM tblTreatment WHERE TreatmentIdentifier = 'T001';");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task InsertAsync_ReturnsGeneratedId()
    {
        SeedPatient(_keepAlive, 1);

        _uow.BeginTransaction();
        var id = await _repository.InsertAsync(BuildTreatment(1));
        _uow.Commit();

        Assert.True(id > 0);
    }

    [Fact]
    public async Task InsertAsync_StoresNullableInvoiceIdAsNull()
    {
        SeedPatient(_keepAlive, 2);

        _uow.BeginTransaction();
        await _repository.InsertAsync(BuildTreatment(2, "T002"));
        _uow.Commit();

        var invoiceId = _keepAlive.ExecuteScalar<int?>("SELECT InvoiceId FROM tblTreatment WHERE TreatmentIdentifier = 'T002';");
        Assert.Null(invoiceId);
    }

    [Fact]
    public async Task InsertAsync_ThrowsOnForeignKeyViolation()
    {
        _uow.BeginTransaction();
        await Assert.ThrowsAsync<SqliteException>(() => _repository.InsertAsync(BuildTreatment(patientId: 999)));
    }
}
