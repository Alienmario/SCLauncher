using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.Shared.Model.Install;
using SCLauncher.Shared.Model.ServerInstall;

namespace SCLauncher.Shared.Service;

public interface IServerInstallService
{
	ServerInstallParams NewInstallParams();

	ServerUninstallParams NewUninstallParams();

	IAsyncEnumerable<object> GetInstaller(ServerInstallParams installParams);

	IAsyncEnumerable<object> GetUninstaller(ServerUninstallParams uninstallParams);

	Task<IDictionary<ServerInstallComponent, ComponentInfo>> GatherComponentInfosAsync(
		ServerInstallParams p, bool checkForUpgrades, CancellationToken ct = default);

	Task<IReadOnlyList<ComponentVersion>> GatherComponentVersionsAsync(
		ServerInstallParams p, ServerInstallComponent component, CancellationToken ct = default);

	bool TryCompareComponentVersions(
		ServerInstallComponent component, string v1, string v2, [NotNullWhen(true)] out int? result);

	Task<ServerAvailability> GetAvailabilityAsync(CancellationToken ct = default);
}
