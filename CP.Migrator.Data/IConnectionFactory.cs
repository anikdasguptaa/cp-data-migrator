using System.Data;

namespace CP.Migrator.Data;

/// <summary>
/// Creates database connections. Implementations encapsulate the connection string
/// and provider-specific details (e.g. SQLite, SQL Server).
/// </summary>
public interface IConnectionFactory
{
    /// <summary>
    /// The fully-formed connection string for this provider.
    /// Exposed here so infrastructure services (e.g. migration runners) can access
    /// the connection string directly without creating a throwaway connection.
    /// </summary>
    string ConnectionString { get; }

    /// <summary>Creates and returns a new (unopened) database connection.</summary>
    IDbConnection CreateConnection();
}
