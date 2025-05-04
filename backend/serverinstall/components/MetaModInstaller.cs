using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

public class MetaModInstaller : IServerComponentInstaller<ComponentInfo>
{
	
	public ServerInstallComponent Type => ServerInstallComponent.MetaMod;
	
	public async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		yield break;
	}

	public Task<ComponentInfo?> GatherInfo(ServerInstallContext ctx, CancellationToken cancellationToken = default)
	{
		return Task.FromResult<ComponentInfo?>(null);
	}

	public Task<bool> ShouldInstall(ServerInstallContext ctx, ComponentInfo? installationInfo)
	{
		return Task.FromResult(true);
	}

}