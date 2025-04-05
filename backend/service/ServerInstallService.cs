using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SCLauncher.backend.serverinstall;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.service;

public class ServerInstallService(
	BackendService backend,
	ServerInstallRunner runner,
	IEnumerable<IServerComponentInstaller<ComponentInfo>> installers)
{

	public ServerInstallParams NewInstallParams()
	{
		return new ServerInstallParams
		{
			Subfolder = backend.GetActiveApp().ServerFolder,
			AppId = backend.GetActiveApp().ServerId
		};
	}

	public IAsyncEnumerable<StatusMessage> GetInstaller(ServerInstallParams installParams)
	{
		return runner.Get(installParams);
	}

	/// Returns components that are currently installed.
	public async Task<IDictionary<ServerInstallComponent, ComponentInfo>> GatherInstalledComponents(ServerInstallParams p)
	{
		var ctx = new ServerInstallContext(p);
		var dict = new Dictionary<ServerInstallComponent, ComponentInfo>();
		
		foreach (var component in Enum.GetValues<ServerInstallComponent>())
		{
			var info = await GatherComponentInfo(component, ctx);
			if (info != null)
			{
				dict.Add(component, info);
			}
		}
		
		return dict;
	}

	/// Returns components that can be installed.
	/// Excludes already installed up-to-date components and those not applicable to current platform.
	public async Task<ISet<ServerInstallComponent>> GatherInstallableComponents(ServerInstallParams p)
	{
		var ctx = new ServerInstallContext(p);
		var set = new HashSet<ServerInstallComponent>();
		
		foreach (var component in Enum.GetValues<ServerInstallComponent>())
		{
			var info = await GatherComponentInfo(component, ctx);
			if (await GetComponentInstaller(component).ShouldInstall(ctx, info))
			{
				set.Add(component);
			}
		}
		
		return set;
	}

	public async Task<ComponentInfo?> GatherComponentInfo(ServerInstallComponent component, ServerInstallParams p)
	{
		return await GatherComponentInfo(component, new ServerInstallContext(p));
	}
	
	private async Task<ComponentInfo?> GatherComponentInfo(ServerInstallComponent component, ServerInstallContext ctx)
	{
		return await GetComponentInstaller(component).GatherInfo(ctx);
	}

	private IServerComponentInstaller<ComponentInfo> GetComponentInstaller(ServerInstallComponent component)
	{
		return installers.First(i => i.Type == component);
	}
	
}