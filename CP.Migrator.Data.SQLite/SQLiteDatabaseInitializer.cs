using DbUp;
using System.Reflection;

namespace CP.Migrator.Data.SQLite;

internal class SQLiteDatabaseInitializer : IDatabaseInitializer
{
    private readonly IConnectionFactory _connectionFactory;

    public SQLiteDatabaseInitializer(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Runs any pending migration scripts against the SQLite database.
    /// Migration history is tracked automatically in the SchemaVersions table using DbUp package.
    /// </summary>
    public void Initialise()
    {
        var upgrader = DeployChanges.To
            .SqliteDatabase(_connectionFactory.ConnectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), s => s.Contains(".Migrations."))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
            throw new InvalidOperationException("Database migration failed.", result.Error);
    }
}
