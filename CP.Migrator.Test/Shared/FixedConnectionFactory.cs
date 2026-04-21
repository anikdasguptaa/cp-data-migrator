using System.Data;
using CP.Migrator.Data;
using Microsoft.Data.Sqlite;

namespace CP.Migrator.Test.Shared;

/// <summary>
/// A test-only IConnectionFactory that always returns a connection to the same
/// in-memory SQLite database, identified by a unique name per test instance.
/// </summary>
internal sealed class FixedConnectionFactory : IConnectionFactory
{
    private readonly string _connectionString;

    public FixedConnectionFactory()
    {
        var dbName = Guid.NewGuid().ToString("N");
        _connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
    }

    public IDbConnection CreateConnection() => new SqliteConnection(_connectionString);
}
