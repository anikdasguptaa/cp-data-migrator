using CP.Migrator.Application;
using CP.Migrator.Application.Startup;
using CP.Migrator.Business;
using CP.Migrator.Data.SQLite;
using CP.Migrator.UI.Forms;
using Microsoft.Extensions.DependencyInjection;
using WinForms = System.Windows.Forms;

namespace CP.Migrator.UI;

internal static class Program
{
	[STAThread]
	static void Main()
	{
		WinForms.Application.SetHighDpiMode(HighDpiMode.SystemAware);
		WinForms.Application.EnableVisualStyles();
		WinForms.Application.SetCompatibleTextRenderingDefault(false);

		var services = new ServiceCollection();
		// Data layer — SQLite database located next to the executable
		services.AddMigratorSQLiteData(Path.Combine(AppContext.BaseDirectory, "CorePractice.db"));
		// Business layer — parsers, validators, auto-fix, export, undo/redo
		services.AddMigratorBusiness();
		// Application layer — pipeline facade, ingestion, startup
		services.AddMigratorApplication();
		// UI
		services.AddScoped<MainForm>();

		using var provider = services.BuildServiceProvider();

		// Ensure the SQLite schema is up to date before showing the UI.
		// IAppStartup is the only Application-layer contract the UI needs for startup.
		provider.GetRequiredService<IAppStartup>().Initialize();

		// MainForm and the pipeline are Scoped — one scope = one application session.
		using var appScope = provider.CreateScope();
		WinForms.Application.Run(appScope.ServiceProvider.GetRequiredService<MainForm>());
	}
}