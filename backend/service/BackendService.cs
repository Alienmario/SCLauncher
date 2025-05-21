using System.IO;
using System.Threading.Tasks;
using SCLauncher.backend.util;
using SCLauncher.model;

namespace SCLauncher.backend.service;

public class BackendService(ConfigHolder config, PersistenceService persistence)
{
	private AppInfo? activeApp;

	public void Initialize()
	{
		persistence.Bind("config", config, JsonSourceGenerationContext.Default);
		
		activeApp = new AppInfo(362890, 346680, "Black Mesa", "Black Mesa Dedicated Server", "bms");

		Task.Run(async () =>
		{
			var steamDir = SteamUtils.FindSteamInstallDir();
			if (steamDir != null)
			{
				config.SteamPath = steamDir;

				if (!Directory.Exists(config.GamePath))
					config.GamePath = await SteamUtils.FindAppPathAsync(steamDir, GetActiveApp().GameAppId)
					                  ?? config.GamePath;

				if (!Directory.Exists(config.ServerPath))
					config.ServerPath = await SteamUtils.FindAppPathAsync(steamDir, GetActiveApp().ServerAppId)
					                    ?? config.ServerPath;
			}
		}).Wait();
	}

	public string? GetSteamDir() => config.SteamPath;

	public AppInfo GetActiveApp() => activeApp!;
}