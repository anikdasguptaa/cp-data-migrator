using CP.Migrator.Business.AutoFix;
using CP.Migrator.Business.Config;
using CP.Migrator.Business.Export;
using CP.Migrator.Business.History;
using CP.Migrator.Business.Ingestion;
using CP.Migrator.Business.Parser;
using CP.Migrator.Business.Validation;
using CP.Migrator.Models.Csv;
using Microsoft.Extensions.DependencyInjection;

namespace CP.Migrator.Business;

/// <summary>
/// Registers all business-layer services (ingestion, parsing, validation,
/// auto-fix, export, undo/redo) against the supplied <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the migrator business services to the DI container.
    /// </summary>
    public static IServiceCollection AddMigratorBusiness(this IServiceCollection services)
    {
        // --- Business services ---
        services.AddScoped<IIngestionService, IngestionService>();

        // --- Parsers ---
        services.AddTransient<ICsvParserService<PatientCsvRow>, PatientCsvParser>();
        services.AddTransient<ICsvParserService<TreatmentCsvRow>, TreatmentCsvParser>();

        // --- Validation options (override via configuration if needed) ---
        services.AddSingleton<PatientValidationOptions>();
        services.AddSingleton<TreatmentValidationOptions>();

        // --- Validators ---
        services.AddTransient<IRecordValidator<PatientCsvRow>, PatientValidator>();
        services.AddTransient<ICrossRecordValidator<TreatmentCsvRow, PatientCsvRow>, TreatmentValidator>();

        // --- Auto-fix ---
        services.AddTransient<IAutoFixService<PatientCsvRow>, PatientAutoFix>();
        services.AddTransient<IAutoFixService<TreatmentCsvRow>, TreatmentAutoFix>();

        // --- Export ---
        services.AddTransient<IRowExportService, RowExportService>();

        // --- History ---
        services.AddSingleton(typeof(IUndoRedoManager<>), typeof(UndoRedoManager<>));

        return services;
    }
}
