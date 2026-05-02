using CP.Migrator.Data.Entities;

namespace CP.Migrator.Data.Repositories;

/// <summary>
/// Repository for <see cref="Patient"/> entities.
/// Provides patient-specific data operations beyond the base insert contract.
/// </summary>
public interface IPatientRepository : IRepository<Patient, int>
{
    /// <summary>
    /// Checks whether a patient with the given composite key already exists in the database.
    /// Returns the existing PatientId if found, or null if no match exists.
    /// </summary>
    Task<int?> FindByCompositeKeyAsync(int clinicId, string patientNo, string firstname, string lastname);
}
