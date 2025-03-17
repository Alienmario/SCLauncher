using System.IO;
using SCLauncher.backend.util;
using SCLauncher.model;

namespace SCLauncher.backend.service;

public class BackendService(ConfigHolder config, PersistenceService persistence)
{
	private string? _steamDir;
	private SteamAppInfo? _activeApp;

	public void Initialize()
	{
		persistence.Bind("config", config, JsonSourceGenerationContext.Default);
		
		_activeApp = new SteamAppInfo(362890, 346680, "Black Mesa", "Black Mesa Dedicated Server");
		_steamDir = SteamUtils.FindSteamInstallDir();

		if (_steamDir != null)
		{
			if (!Directory.Exists(config.GamePath))
				config.GamePath = SteamUtils.FindAppPath(_steamDir, GetActiveApp().Id) ?? config.GamePath;
			
			if (!Directory.Exists(config.ServerPath))
				config.ServerPath = SteamUtils.FindAppPath(_steamDir, GetActiveApp().ServerId) ?? config.ServerPath;
		}
	}

	public string? GetSteamDir() => _steamDir;

	public SteamAppInfo GetActiveApp() => _activeApp!;
}