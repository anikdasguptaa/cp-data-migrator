namespace CP.Migrator.Data.Repositories;

/// <summary>
/// Base repository contract for all entities.
/// <typeparam name="T">The entity type. Must be a reference type.</typeparam>
/// <typeparam name="TKey">
/// The type of the database-generated primary key returned after insert
/// (e.g. <c>int</c> for AUTOINCREMENT columns, <c>Guid</c> for UUID keys).
/// Keeping this generic means new entities with different key strategies
/// can implement this interface without changing the base contract.
/// </typeparam>
/// </summary>
/// <remarks>
/// Repositories obtain their connection and transaction from an <see cref="IUnitOfWork"/>
/// injected via the constructor.  Method signatures stay clean — callers never pass
/// infrastructure objects.  The UoW controls whether a transaction is active.
/// </remarks>
public interface IRepository<T, TKey> where T : class
{
    /// <summary>
    /// Inserts a single entity and returns the database-generated primary key.
    /// Uses the connection and (optional) transaction from the injected
    /// <see cref="IUnitOfWork"/>.
    /// </summary>
    Task<TKey> InsertAsync(T entity);
}
