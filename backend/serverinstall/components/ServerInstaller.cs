using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

public class ServerInstaller : IServerComponentInstaller<ComponentInfo>
{
	
	public ServerInstallComponent Type => ServerInstallComponent.Server;
	
	public async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (ctx.Params.Method == ServerInstallMethod.Steam)
		{
			SteamUtils.InstallApp(ctx.Params.AppId);
			yield return new StatusMessage("Waiting for Steam to finish downloading");
		}
		else
		{
			yield return new StatusMessage("ServerInstaller");
		}
	}

	public Task<ComponentInfo?> GatherInfo(ServerInstallContext ctx)
	{
		return Task.FromResult<ComponentInfo?>(null);
	}

	public Task<bool> ShouldInstall(ServerInstallContext ctx, ComponentInfo? installationInfo)
	{
		return Task.FromResult(true);
	}

}