using CP.Migrator.Data;
using CP.Migrator.Data.SQLite;
using CP.Migrator.Data.SQLite.Repositories;
using CP.Migrator.Models.Entities;
using CP.Migrator.Test.Shared;
using Dapper;
using System.Data;

namespace CP.Migrator.Test.Integration.SQLite.Repositories;

public class SQLitePatientRepositoryTests : IDisposable
{
    private readonly FixedConnectionFactory _factory = new();
    private readonly IDbConnection _keepAlive;
    private readonly IUnitOfWork _uow;
    private readonly SQLitePatientRepository _repository;

    public SQLitePatientRepositoryTests()
    {
        _keepAlive = _factory.CreateConnection();
        _keepAlive.Open();
        new SQLiteDatabaseInitializer(_factory).Initialise();
        _uow = new SQLiteUnitOfWork(_factory);
        _repository = new SQLitePatientRepository(_uow);
    }

    public void Dispose()
    {
        _uow.Dispose();
        _keepAlive.Dispose();
    }

    private static Patient BuildPatient(string identifier = "P001") => new()
    {
        PatientIdentifier = identifier,
        PatientNo = "100",
        Firstname = "John",
        Lastname = "Doe",
        Middlename = null,
        PreferredName = "Johnny",
        DateOfBirth = "1990-01-01",
        Title = "Mr",
        Sex = "M",
        Email = "john@example.com",
        HomePhone = "0712345678",
        Mobile = "0412345678",
        Occupation = "Engineer",
        CompanyName = null,
        AddressLine1 = "1 Main St",
        AddressLine2 = null,
        Suburb = "Sydney",
        Postcode = "2000",
        State = "NSW",
        Country = "Australia",
        ClinicId = 1,
        IsDeleted = 0
    };

    [Fact]
    public async Task InsertAsync_InsertsPatientRow()
    {
        _uow.BeginTransaction();
        await _repository.InsertAsync(BuildPatient());
        _uow.Commit();

        var count = _keepAlive.ExecuteScalar<int>("SELECT COUNT(*) FROM tblPatient WHERE PatientIdentifier = 'P001';");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task InsertAsync_ReturnsGeneratedId()
    {
        _uow.BeginTransaction();
        var id = await _repository.InsertAsync(BuildPatient());
        _uow.Commit();

        Assert.True(id > 0);
    }

    [Fact]
    public async Task InsertAsync_StoresNullableFieldsAsNull()
    {
        _uow.BeginTransaction();
        await _repository.InsertAsync(BuildPatient("P002"));
        _uow.Commit();

        var middlename = _keepAlive.ExecuteScalar<string?>("SELECT Middlename FROM tblPatient WHERE PatientIdentifier = 'P002';");
        Assert.Null(middlename);
    }

    [Fact]
    public async Task InsertAsync_StoresCorrectValues()
    {
        var patient = BuildPatient("P003");
        _uow.BeginTransaction();
        await _repository.InsertAsync(patient);
        _uow.Commit();

        var result = _keepAlive.QuerySingle<Patient>("SELECT * FROM tblPatient WHERE PatientIdentifier = 'P003';");
        Assert.Equal(patient.Firstname, result.Firstname);
        Assert.Equal(patient.Lastname, result.Lastname);
        Assert.Equal(patient.Email, result.Email);
        Assert.Equal(patient.IsDeleted, result.IsDeleted);
    }
}
