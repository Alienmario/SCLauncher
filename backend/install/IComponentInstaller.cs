using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.model;
using SCLauncher.model.install;

namespace SCLauncher.backend.install;

public interface IComponentInstaller<TComponentInfo, TInstallContext> where TComponentInfo : ComponentInfo
{
	
	public IAsyncEnumerable<StatusMessage> Install(TInstallContext ctx, CancellationToken cancellationToken);
	
	/// Returns information about current installation of the component.
	public Task<TComponentInfo?> GatherInfo(TInstallContext ctx, CancellationToken cancellationToken = default);

	/// Returns whether the component should be installed. Includes OS and current installation checks as necessary.
	public Task<bool> ShouldInstall(TInstallContext ctx, TComponentInfo? info)
	{
		return Task.FromResult(info == null);
	}
	
}