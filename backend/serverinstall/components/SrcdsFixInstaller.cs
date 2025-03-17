using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

public class SrcdsFixInstaller : IServerComponentInstaller<ComponentInfo>
{
	
	public ServerInstallComponent Type => ServerInstallComponent.SrcdsFix;
	
	public async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		yield break;
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