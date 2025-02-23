using SCLauncher.backend.util;
using SCLauncher.model;

namespace SCLauncher.backend.service;

public class BackendService(ConfigHolder config)
{
	private string? _steamDir;
	private SteamAppInfo? _activeApp;

	public void Initialize()
	{
		_activeApp = new SteamAppInfo(362890, 346680, "Black Mesa", "Black Mesa Dedicated Server");
		_steamDir = SteamUtils.FindSteamInstallDir();

		if (_steamDir != null)
		{
			config.GamePath = SteamUtils.FindAppPath(_steamDir, GetActiveApp().Id);
			config.ServerPath = SteamUtils.FindAppPath(_steamDir, GetActiveApp().ServerId);
		}
	}

	public string? GetSteamDir()
	{
		return _steamDir;
	}
	
	public SteamAppInfo GetActiveApp()
	{
		return _activeApp!;
	}

}