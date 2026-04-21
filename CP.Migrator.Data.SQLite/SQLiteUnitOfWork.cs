using System.Data;

namespace CP.Migrator.Data.SQLite;

/// <summary>
/// SQLite implementation of <see cref="IUnitOfWork"/>.
/// Opens a connection on construction; disposing cleans up both
/// the transaction (if active) and the connection.
/// </summary>
internal sealed class SQLiteUnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public SQLiteUnitOfWork(IConnectionFactory connectionFactory)
    {
        _connection = connectionFactory.CreateConnection();
        _connection.Open();
    }

    public IDbConnection Connection => _connection;

    public IDbTransaction? Transaction => _transaction;

    public void BeginTransaction()
    {
        if (_transaction is not null)
            throw new InvalidOperationException("A transaction is already active on this unit of work.");

        _transaction = _connection.BeginTransaction();
    }

    public void Commit()
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        _transaction.Commit();
        _transaction.Dispose();
        _transaction = null;
    }

    public void Rollback()
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to roll back.");

        _transaction.Rollback();
        _transaction.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _transaction?.Dispose();
        _connection.Dispose();
    }
}
