using System.Data;

namespace CP.Migrator.Data;

/// <summary>
/// Creates database connections. Implementations encapsulate the connection string
/// and provider-specific details (e.g. SQLite, SQL Server).
/// </summary>
public interface IConnectionFactory
{
    /// <summary>Creates and returns a new (unopened) database connection.</summary>
    IDbConnection CreateConnection();
}
