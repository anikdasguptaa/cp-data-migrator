using System.Data;

namespace CP.Migrator.Data;

/// <summary>
/// Represents a unit of work — a single logical session with the database.
/// Owns the <see cref="IDbConnection"/> and optionally an <see cref="IDbTransaction"/>.
///
/// <para><b>Design rationale</b></para>
/// <list type="bullet">
///   <item>Repositories receive this via constructor injection and never manage
///         connections or transactions themselves.</item>
///   <item>For read-only operations, <see cref="Transaction"/> is <c>null</c> —
///         repositories simply pass it through to Dapper which treats null as
///         "no transaction".</item>
///   <item>For transactional writes, the orchestrating service calls
///         <see cref="BeginTransaction"/> before the work and
///         <see cref="Commit"/> / <see cref="Rollback"/> after.</item>
///   <item>Repository method signatures stay clean — no connection/transaction
///         parameters — yet all operations within one UoW instance share the
///         same connection and (when active) the same transaction.</item>
/// </list>
///
/// <para><b>Lifetime</b></para>
/// Register as <b>Scoped</b> (or create manually per operation).  One UoW
/// instance = one open connection = one (optional) transaction.  Disposing the
/// UoW disposes the connection and any uncommitted transaction.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>The shared, open database connection for this session.</summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// The active transaction, or <c>null</c> if no transaction has been started.
    /// Repositories should pass this to every Dapper call — Dapper ignores a
    /// <c>null</c> transaction, so read-only code works without any special handling.
    /// </summary>
    IDbTransaction? Transaction { get; }

    /// <summary>Opens a transaction on the shared connection.</summary>
    /// <exception cref="InvalidOperationException">A transaction is already active.</exception>
    void BeginTransaction();

    /// <summary>Commits the active transaction.</summary>
    void Commit();

    /// <summary>Rolls back the active transaction.</summary>
    void Rollback();
}
