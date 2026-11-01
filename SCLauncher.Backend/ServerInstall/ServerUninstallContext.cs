using SCLauncher.Shared.Model.ServerInstall;

namespace SCLauncher.Backend.ServerInstall;

internal class ServerUninstallContext(ServerUninstallParams p)
{
	public ServerUninstallParams Params { get; } = p;
}