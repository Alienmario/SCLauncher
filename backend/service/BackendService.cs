using System.IO;
using SCLauncher.backend.util.steam;
using SCLauncher.model;

namespace SCLauncher.backend.service;

public class BackendService(ConfigHolder config, PersistenceService persistence)
{
	private SteamAppInfo? activeApp;

	public void Initialize()
	{
		persistence.Bind("config", config, JsonSourceGenerationContext.Default);
		
		activeApp = new SteamAppInfo(362890, 346680, "Black Mesa", "Black Mesa Dedicated Server");

		var steamDir = SteamUtils.FindSteamInstallDir();
		if (steamDir != null)
		{
			config.SteamPath = steamDir;
			
			if (!Directory.Exists(config.GamePath))
				config.GamePath = SteamUtils.FindAppPathAsync(steamDir, GetActiveApp().Id).GetAwaiter().GetResult()
				                  ?? config.GamePath;
			
			if (!Directory.Exists(config.ServerPath))
				config.ServerPath = SteamUtils.FindAppPathAsync(steamDir, GetActiveApp().ServerId).GetAwaiter().GetResult()
				                    ?? config.ServerPath;
		}
	}

	public string? GetSteamDir() => config.SteamPath;

	public SteamAppInfo GetActiveApp() => activeApp!;
}