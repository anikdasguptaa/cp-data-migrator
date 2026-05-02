using CP.Migrator.Data;

namespace CP.Migrator.Application.Startup
{
	internal sealed class AppStartup : IAppStartup
	{
		private readonly IDatabaseInitializer _dbInitializer;

		public AppStartup(IDatabaseInitializer dbInitializer)
			=> _dbInitializer = dbInitializer;

		/// <inheritdoc />
		public void Initialize() => _dbInitializer.Initialise();
	}
}
