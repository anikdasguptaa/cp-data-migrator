using CP.Migrator.Data;
using CP.Migrator.Data.SQLite;
using CP.Migrator.Data.SQLite.Repositories;
using CP.Migrator.Models.Entities;
using CP.Migrator.Test.Shared;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace CP.Migrator.Test.Integration.SQLite.Repositories;

public class SQLiteInvoiceRepositoryTests : IDisposable
{
    private readonly FixedConnectionFactory _factory = new();
    private readonly IDbConnection _keepAlive;
    private readonly IUnitOfWork _uow;
    private readonly SQLiteInvoiceRepository _repository;

    public SQLiteInvoiceRepositoryTests()
    {
        _keepAlive = _factory.CreateConnection();
        _keepAlive.Open();
        new SQLiteDatabaseInitializer(_factory).Initialise();
        _uow = new SQLiteUnitOfWork(_factory);
        _repository = new SQLiteInvoiceRepository(_uow);
    }

    public void Dispose()
    {
        _uow.Dispose();
        _keepAlive.Dispose();
    }

    private static void SeedPatient(IDbConnection connection, int patientId)
    {
        connection.Execute(
            $"INSERT INTO tblPatient (PatientId, PatientIdentifier, Firstname, Lastname, ClinicId, IsDeleted) VALUES ({patientId}, 'P{patientId}', 'John', 'Doe', 1, 0);");
    }

    private static Invoice BuildInvoice(int patientId, string identifier = "INV001") => new()
    {
        InvoiceIdentifier = identifier,
        InvoiceNo = 1001,
        InvoiceDate = "2024-01-01",
        DueDate = "2024-01-15",
        Note = "Test invoice",
        Total = 250.00m,
        Paid = 100.00m,
        Discount = null,
        PatientId = patientId,
        ClinicId = 1,
        IsDeleted = 0
    };

    [Fact]
    public async Task InsertAsync_InsertsInvoiceRow()
    {
        SeedPatient(_keepAlive, 1);

        _uow.BeginTransaction();
        await _repository.InsertAsync(BuildInvoice(1));
        _uow.Commit();

        var count = _keepAlive.ExecuteScalar<int>("SELECT COUNT(*) FROM tblInvoice WHERE InvoiceIdentifier = 'INV001';");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task InsertAsync_ReturnsGeneratedId()
    {
        SeedPatient(_keepAlive, 1);

        _uow.BeginTransaction();
        var id = await _repository.InsertAsync(BuildInvoice(1));
        _uow.Commit();

        Assert.True(id > 0);
    }

    [Fact]
    public async Task InsertAsync_StoresNullableFieldsAsNull()
    {
        SeedPatient(_keepAlive, 2);

        _uow.BeginTransaction();
        await _repository.InsertAsync(BuildInvoice(2, "INV002"));
        _uow.Commit();

        var discount = _keepAlive.ExecuteScalar<decimal?>("SELECT Discount FROM tblInvoice WHERE InvoiceIdentifier = 'INV002';");
        Assert.Null(discount);
    }

    [Fact]
    public async Task InsertAsync_ThrowsOnForeignKeyViolation()
    {
        _uow.BeginTransaction();
        await Assert.ThrowsAsync<SqliteException>(() => _repository.InsertAsync(BuildInvoice(patientId: 999)));
    }
}
