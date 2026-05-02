using CP.Migrator.Data.Entities;
using CP.Migrator.Data.Repositories;
using Dapper;

namespace CP.Migrator.Data.SQLite.Repositories;

/// <summary>
/// SQLite/Dapper implementation of <see cref="IPatientRepository"/>.
/// </summary>
internal class SQLitePatientRepository : IPatientRepository
{
    private readonly IUnitOfWork _uow;

    public SQLitePatientRepository(IUnitOfWork uow) => _uow = uow;

    public async Task<int?> FindByCompositeKeyAsync(int clinicId, string patientNo, string firstname, string lastname)
    {
        const string sql = """
            SELECT PatientId FROM tblPatient
            WHERE ClinicId = @clinicId
              AND PatientNo = @patientNo
              AND Firstname = @firstname
              AND Lastname = @lastname
            LIMIT 1;
            """;

        return await _uow.Connection.QueryFirstOrDefaultAsync<int?>(sql,
            new { clinicId, patientNo, firstname, lastname }, _uow.Transaction);
    }

    public async Task<int> InsertAsync(Patient entity)
    {
        const string sql = """
            INSERT INTO tblPatient (
                PatientIdentifier, PatientNo, Firstname, Lastname, Middlename, PreferredName,
                DateOfBirth, Title, Sex, Email, HomePhone, Mobile, Occupation, CompanyName,
                AddressLine1, AddressLine2, Suburb, Postcode, State, Country, ClinicId, IsDeleted)
            VALUES (
                @PatientIdentifier, @PatientNo, @Firstname, @Lastname, @Middlename, @PreferredName,
                @DateOfBirth, @Title, @Sex, @Email, @HomePhone, @Mobile, @Occupation, @CompanyName,
                @AddressLine1, @AddressLine2, @Suburb, @Postcode, @State, @Country, @ClinicId, @IsDeleted);
            SELECT last_insert_rowid();
            """;

        return await _uow.Connection.ExecuteScalarAsync<int>(sql, entity, _uow.Transaction);
    }
}
