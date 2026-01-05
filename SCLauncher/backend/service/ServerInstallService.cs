using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.backend.serverinstall;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.service;

public class ServerInstallService(
	BackendService backend,
	ServerInstallRunner serverInstallRunner,
	IEnumerable<IServerComponentInstaller<ComponentInfo>> componentInstallers)
{

	public ServerInstallParams NewInstallParams()
	{
		return new ServerInstallParams
		{
			Profile = backend.ActiveProfile
		};
	}

	public ServerUninstallParams NewUninstallParams()
	{
		return new ServerUninstallParams
		{
			Profile = backend.ActiveProfile,
			Path = backend.ActiveProfile.ServerPath ?? string.Empty
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
		
		foreach (var installer in componentInstallers.OrderBy(installer => installer.Component.InstallOrder))
		{
			ctx.ComponentInfos[installer.Component] = await installer.GatherInfoAsync(ctx, checkForUpgrades, ct);
		}
		
		return ctx.ComponentInfos;
	}

	internal IServerComponentInstaller<ComponentInfo> GetComponentInstaller(ServerInstallComponent component)
	{
		return componentInstallers.First(installer => installer.Component == component);
	}
	
}