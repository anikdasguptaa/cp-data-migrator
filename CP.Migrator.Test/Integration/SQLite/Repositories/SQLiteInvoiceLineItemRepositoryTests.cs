using CP.Migrator.Data;
using CP.Migrator.Data.Entities;
using CP.Migrator.Data.SQLite;
using CP.Migrator.Data.SQLite.Repositories;
using CP.Migrator.Test.Shared;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace CP.Migrator.Test.Integration.SQLite.Repositories;

public class SQLiteInvoiceLineItemRepositoryTests : IDisposable
{
    private readonly FixedConnectionFactory _factory = new();
    private readonly IDbConnection _keepAlive;
    private readonly IUnitOfWork _uow;
    private readonly SQLiteInvoiceLineItemRepository _repository;

    public SQLiteInvoiceLineItemRepositoryTests()
    {
        _keepAlive = _factory.CreateConnection();
        _keepAlive.Open();
        new SQLiteDatabaseInitializer(_factory).Initialise();
        _uow = new SQLiteUnitOfWork(_factory);
        _repository = new SQLiteInvoiceLineItemRepository(_uow);
    }

    public void Dispose()
    {
        _uow.Dispose();
        _keepAlive.Dispose();
    }

    private static void SeedPatientInvoiceTreatment(IDbConnection connection, int patientId, int invoiceId, int treatmentId)
    {
        connection.Execute(
            $"INSERT INTO tblPatient (PatientId, PatientIdentifier, Firstname, Lastname, ClinicId, IsDeleted) VALUES ({patientId}, 'P{patientId}', 'Jane', 'Doe', 1, 0);");
        connection.Execute(
            $"INSERT INTO tblInvoice (InvoiceId, InvoiceIdentifier, InvoiceNo, Total, PatientId, ClinicId, IsDeleted) VALUES ({invoiceId}, 'INV{invoiceId}', {invoiceId}, 300.00, {patientId}, 1, 0);");
        connection.Execute(
            $"INSERT INTO tblTreatment (TreatmentId, TreatmentIdentifier, Description, ItemCode, Quantity, Fee, PatientId, ClinicId, IsPaid, IsVoided) VALUES ({treatmentId}, 'T{treatmentId}', 'Filling', 'D2140', 1, 150.00, {patientId}, 1, 0, 0);");
    }

    private static InvoiceLineItem BuildLineItem(int patientId, int invoiceId, int treatmentId, string identifier = "LI001") => new()
    {
        InvoiceLineItemIdentifier = identifier,
        Description = "Filling procedure",
        ItemCode = "D2140",
        Quantity = 1,
        UnitAmount = 150.00m,
        LineAmount = 150.00m,
        PatientId = patientId,
        TreatmentId = treatmentId,
        InvoiceId = invoiceId,
        ClinicId = 1
    };

    [Fact]
    public async Task InsertAsync_InsertsLineItemRow()
    {
        SeedPatientInvoiceTreatment(_keepAlive, 1, 1, 1);

        _uow.BeginTransaction();
        await _repository.InsertAsync(BuildLineItem(1, 1, 1));
        _uow.Commit();

        var count = _keepAlive.ExecuteScalar<int>("SELECT COUNT(*) FROM tblInvoiceLineItem WHERE InvoiceLineItemIdentifier = 'LI001';");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task InsertAsync_ReturnsGeneratedId()
    {
        SeedPatientInvoiceTreatment(_keepAlive, 1, 1, 1);

        _uow.BeginTransaction();
        var id = await _repository.InsertAsync(BuildLineItem(1, 1, 1));
        _uow.Commit();

        Assert.True(id > 0);
    }

    [Fact]
    public async Task InsertAsync_StoresCorrectValues()
    {
        SeedPatientInvoiceTreatment(_keepAlive, 1, 1, 1);

        var lineItem = BuildLineItem(1, 1, 1, "LI002");
        _uow.BeginTransaction();
        await _repository.InsertAsync(lineItem);
        _uow.Commit();

        var result = _keepAlive.QuerySingle<InvoiceLineItem>("SELECT * FROM tblInvoiceLineItem WHERE InvoiceLineItemIdentifier = 'LI002';");
        Assert.Equal(lineItem.UnitAmount, result.UnitAmount);
        Assert.Equal(lineItem.LineAmount, result.LineAmount);
        Assert.Equal(lineItem.ItemCode, result.ItemCode);
    }

    [Fact]
    public async Task InsertAsync_ThrowsOnForeignKeyViolation()
    {
        _uow.BeginTransaction();
        await Assert.ThrowsAsync<SqliteException>(() => _repository.InsertAsync(BuildLineItem(patientId: 999, invoiceId: 999, treatmentId: 999)));
    }
}
