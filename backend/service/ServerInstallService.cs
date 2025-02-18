using SCLauncher.backend.util;
using SCLauncher.model;

namespace SCLauncher.backend.service;

public class ServerInstallService(
	ConfigHolder config,
	BackendService backendService)
{
	
	public void StartSteamInstall()
	{
		SteamUtils.InstallApp(backendService.GetServerAppId());
	}
	
}