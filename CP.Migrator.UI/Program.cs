using CP.Migrator.Business;
using CP.Migrator.Business.AutoFix;
using CP.Migrator.Business.Export;
using CP.Migrator.Business.History;
using CP.Migrator.Business.Parser;
using CP.Migrator.Business.Validation;
using CP.Migrator.Data;
using CP.Migrator.Data.SQLite;
using CP.Migrator.Models.Csv;
using CP.Migrator.UI.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace CP.Migrator.UI;

internal static class Program
{
	[STAThread]
	static void Main()
	{
		ApplicationConfiguration.Initialize();

		var services = new ServiceCollection();
		ConfigureServices(services);

		using var provider = services.BuildServiceProvider();

		// Ensure the SQLite schema is up to date before showing the UI.
		provider.GetRequiredService<IDatabaseInitializer>().Initialise();

		var mainForm = new MainForm(
			provider.GetRequiredService<ICsvParserService<PatientCsvRow>>(),
			provider.GetRequiredService<ICsvParserService<TreatmentCsvRow>>(),
			provider.GetRequiredService<IRecordValidator<PatientCsvRow>>(),
			provider.GetRequiredService<ICrossRecordValidator<TreatmentCsvRow, PatientCsvRow>>(),
			provider.GetRequiredService<IAutoFixService<PatientCsvRow>>(),
			provider.GetRequiredService<IAutoFixService<TreatmentCsvRow>>(),
			provider.GetRequiredService<IRowExportService>(),
			provider.GetRequiredService<IUndoRedoManager<PatientCsvRow>>(),
			provider.GetRequiredService<IUndoRedoManager<TreatmentCsvRow>>(),
			provider
		);

		Application.Run(mainForm);
	}

	private static void ConfigureServices(IServiceCollection services)
	{
		// Data layer — SQLite database located next to the executable.
		services.AddSQLiteData(Path.Combine(AppContext.BaseDirectory, "CorePractice.db"));

		// Business layer — parsers, validators, auto-fix, ingestion, export, undo/redo
		services.AddMigratorBusiness();
	}
}