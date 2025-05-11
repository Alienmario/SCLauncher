using System.IO;
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

		var steamDir = SteamUtils.FindSteamInstallDir();
		if (steamDir != null)
		{
			config.SteamPath = steamDir;
			
			if (!Directory.Exists(config.GamePath))
				config.GamePath = SteamUtils.FindAppPathAsync(steamDir, GetActiveApp().GameAppId).GetAwaiter().GetResult()
				                  ?? config.GamePath;
			
			if (!Directory.Exists(config.ServerPath))
				config.ServerPath = SteamUtils.FindAppPathAsync(steamDir, GetActiveApp().ServerAppId).GetAwaiter().GetResult()
				                    ?? config.ServerPath;
		}
	}

	public string? GetSteamDir() => config.SteamPath;

	public AppInfo GetActiveApp() => activeApp!;
}