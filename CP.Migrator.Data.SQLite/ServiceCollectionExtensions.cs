using CP.Migrator.Data.Repositories;
using CP.Migrator.Data.SQLite.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CP.Migrator.Data.SQLite;

/// <summary>
/// Registers the SQLite data-layer services (connection factory, database
/// initializer, unit of work, and repositories).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the SQLite data-access services to the DI container.
    /// </summary>
    /// <param name="services">The DI container builder.</param>
    /// <param name="databasePath">Full path to the SQLite database file.</param>
    public static IServiceCollection AddSQLiteData(
        this IServiceCollection services,
        string databasePath)
    {
        // --- Data / Infrastructure ---
        services.AddSingleton<IConnectionFactory>(_ => new SQLiteConnectionFactory(databasePath));
        services.AddSingleton<IDatabaseInitializer, SQLiteDatabaseInitializer>();

        // UoW is Scoped: one connection + one (optional) transaction per operation.
        // All repositories injected in the same scope share the same UoW instance.
        services.AddScoped<IUnitOfWork, SQLiteUnitOfWork>();

        // --- Repositories (Scoped so they share the same UoW) ---
        services.AddScoped<IPatientRepository, SQLitePatientRepository>();
        services.AddScoped<IInvoiceRepository, SQLiteInvoiceRepository>();
        services.AddScoped<ITreatmentRepository, SQLiteTreatmentRepository>();
        services.AddScoped<IInvoiceLineItemRepository, SQLiteInvoiceLineItemRepository>();

        return services;
    }
}
