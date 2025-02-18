using SCLauncher.backend.util;
using SCLauncher.model;

namespace SCLauncher.backend.service;

public class BackendService(ConfigHolder config)
{
	private string? _steamDir;

	public void Initialize()
	{
		_steamDir = SteamUtils.FindSteamInstallDir();

		if (_steamDir != null)
		{
			config.GamePath = SteamUtils.FindAppPath(_steamDir, GetGameAppId());
			config.ServerPath = SteamUtils.FindAppPath(_steamDir, GetServerAppId());
		}
	}

	public string? GetSteamDir()
	{
		return _steamDir;
	}
	
	public int GetGameAppId()
	{
		return 362890;
	}
	
	public int GetServerAppId()
	{
		return 346680;
	}

}