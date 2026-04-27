using CP.Migrator.Application.Ingestion;
using CP.Migrator.Application.Pipeline;
using CP.Migrator.Application.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace CP.Migrator.Application;

/// <summary>
/// Registers all Application-layer services against the supplied
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the pipeline facade and its dependencies to the DI container.
    /// </summary>
    public static IServiceCollection AddMigratorApplication(this IServiceCollection services)
    {
        // Startup — wraps IDatabaseInitializer so the UI depends only on IAppStartup
        services.AddSingleton<IAppStartup, AppStartup>();

        // Ingestion is Scoped: it owns a UoW/connection for the lifetime of one import run
        services.AddScoped<IIngestionService, IngestionService>();

        // Pipeline is Scoped to match IIngestionService (same lifetime as one import session)
        services.AddScoped<IMigratorPipelineService, MigratorPipelineService>();

        return services;
    }
}
