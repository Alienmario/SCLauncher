using System;
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
			AppInfo = backend.GetActiveApp()
		};
	}

	public IAsyncEnumerable<StatusMessage> GetInstaller(ServerInstallParams installParams)
	{
		return serverInstallRunner.Get(installParams);
	}

	public async Task<IDictionary<ServerInstallComponent, ComponentInfo>> GatherComponentInfoAsync(
		ServerInstallParams p,
		bool checkForUpgrades,
		CancellationToken cancellationToken = default)
	{
		var ctx = new ServerInstallContext(p);
		
		foreach (var component in Enum.GetValues<ServerInstallComponent>())
		{
			await GatherComponentInfoAsync(component, ctx, checkForUpgrades, cancellationToken);
		}
		
		return ctx.ComponentInfos;
	}
	
	internal async Task GatherComponentInfoAsync(
		ServerInstallComponent component,
		ServerInstallContext ctx,
		bool checkForUpgrades,
		CancellationToken cancellationToken = default)
	{
		ctx.ComponentInfos[component] = await GetComponentInstaller(component)
			.GatherInfoAsync(ctx, checkForUpgrades, cancellationToken);
	}

	internal IServerComponentInstaller<ComponentInfo> GetComponentInstaller(ServerInstallComponent component)
	{
		return componentInstallers.First(i => i.ComponentType == component);
	}
	
}