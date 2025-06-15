using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.model;
using SCLauncher.model.install;

namespace SCLauncher.backend.install;

public interface IComponentInstaller<TComponentInfo, TInstallContext, TUninstallContext> where TComponentInfo : ComponentInfo
{
	
	/// The async-enumerable component installer
	public IAsyncEnumerable<StatusMessage> Install(TInstallContext ctx, CancellationToken ct = default);

	public IAsyncEnumerable<StatusMessage> Uninstall(TUninstallContext ctx, CancellationToken ct = default)
	{
		throw new NotImplementedException("Uninstall method is not implemented for this component installer.");
	}

	/// Returns information about current installation of the component and whether it can be installed/upgraded.
	public Task<TComponentInfo> GatherInfoAsync(TInstallContext ctx, bool checkForUpgrades, CancellationToken ct = default);
	
}