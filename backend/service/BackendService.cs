using System.IO;
using SCLauncher.backend.util;
using SCLauncher.model;

namespace SCLauncher.backend.service;

public class BackendService(ConfigHolder config, PersistenceService persistence)
{
	private string? steamDir;
	private SteamAppInfo? activeApp;

	public void Initialize()
	{
		persistence.Bind("config", config, JsonSourceGenerationContext.Default);
		
		activeApp = new SteamAppInfo(362890, 346680, "Black Mesa", "Black Mesa Dedicated Server");
		steamDir = SteamUtils.FindSteamInstallDir();

		if (steamDir != null)
		{
			if (!Directory.Exists(config.GamePath))
				config.GamePath = SteamUtils.FindAppPath(steamDir, GetActiveApp().Id) ?? config.GamePath;
			
			if (!Directory.Exists(config.ServerPath))
				config.ServerPath = SteamUtils.FindAppPath(steamDir, GetActiveApp().ServerId) ?? config.ServerPath;
		}
	}

	public string? GetSteamDir() => steamDir;

	public SteamAppInfo GetActiveApp() => activeApp!;
}