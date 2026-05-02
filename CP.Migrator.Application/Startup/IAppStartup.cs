namespace CP.Migrator.Application.Startup
{
	/// <summary>
	/// Encapsulates all application startup steps (e.g. database initialisation)
	/// so the UI only depends on this single Application-layer contract.
	/// </summary>
	public interface IAppStartup
	{
		/// <summary>Runs all startup tasks before the UI is shown.</summary>
		void Initialize();
	}
}
