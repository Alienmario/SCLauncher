using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.backend.serverinstall;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.model.config;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.service;

public class ServerInstallService(
	ProfilesService profilesService,
	ServerInstallRunner serverInstallRunner,
	IEnumerable<IServerComponentInstaller<ComponentInfo>> componentInstallers)
{

	public ServerInstallParams NewInstallParams()
	{
		AppProfile profile = profilesService.ActiveProfile;

		if (!Directory.Exists(profile.ServerPath))
		{
			return new ServerInstallParams
			{
				Profile = profile
			};
		}

		bool steam = SteamUtils.IsPathSteamApp(profile.ServerPath, profile.ServerAppId);
		return new ServerInstallParams
		{
			Profile = profile,
			Method = steam ? ServerInstallMethod.Steam : ServerInstallMethod.External,
			Path = profile.ServerPath,
			CreateSubfolder = false
		};
	}

	public ServerUninstallParams NewUninstallParams()
	{
		return new ServerUninstallParams
		{
			Profile = profilesService.ActiveProfile,
			Path = profilesService.ActiveProfile.ServerPath ?? string.Empty
		};
	}

	public IAsyncEnumerable<StatusMessage> GetInstaller(ServerInstallParams installParams)
	{
		return serverInstallRunner.Installer(installParams);
	}
	
	public IAsyncEnumerable<StatusMessage> GetUninstaller(ServerUninstallParams uninstallParams)
	{
		return serverInstallRunner.Uninstaller(uninstallParams);
	}

	public async Task<IDictionary<ServerInstallComponent, ComponentInfo>> GatherComponentInfosAsync(
		ServerInstallParams p, bool checkForUpgrades, CancellationToken ct = default)
	{
		var ctx = new ServerInstallContext(p);
		
		foreach (var installer in componentInstallers
			         .Where(installer => installer.Component.AppTypes.Contains(p.Profile.AppType))
			         .OrderBy(installer => installer.Component.InstallOrder))
		{
			ctx.ComponentInfos[installer.Component] = await installer.GatherInfoAsync(ctx, checkForUpgrades, ct);
		}
		
		return ctx.ComponentInfos;
	}
	
	public async Task<ServerAvailability> GetAvailabilityAsync(CancellationToken ct = default)
	{
		AppProfile profile = profilesService.ActiveProfile;
		if (string.IsNullOrWhiteSpace(profile.ServerPath))
			return ServerAvailability.Unavailable;

		var infos = await GatherComponentInfosAsync(new ServerInstallParams
		{
			Profile = profile,
			Method = ServerInstallMethod.External,
			Path = profile.ServerPath,
			CreateSubfolder = false
		}, false, ct);

		var required = infos.Where(e => e.Key.Required && e.Value.Installable).ToList();
		if (required.All(e => e.Value.Installed))
			return ServerAvailability.Available;
		if (required.Any(e => e.Value.Installed))
			return ServerAvailability.PartiallyInstalled;
		return ServerAvailability.Unavailable;
	}
	
}