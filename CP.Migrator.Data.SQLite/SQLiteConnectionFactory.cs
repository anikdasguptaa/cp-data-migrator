using Microsoft.Data.Sqlite;
using System.Data;

namespace CP.Migrator.Data.SQLite;

/// <summary>
/// Creates SQLite connections using Microsoft.Data.Sqlite.
/// </summary>
internal class SQLiteConnectionFactory : IConnectionFactory
{
    private readonly string _connectionString;

    public SQLiteConnectionFactory(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true
        }.ToString();
    }

    public string ConnectionString => _connectionString;

    public IDbConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }
}
