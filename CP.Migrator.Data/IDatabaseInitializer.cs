namespace CP.Migrator.Data;

/// <summary>
/// Responsible for ensuring the database schema is up to date.
/// Implementations run migration scripts on application startup.
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>Runs any pending migration scripts against the database.</summary>
    void Initialise();
}
