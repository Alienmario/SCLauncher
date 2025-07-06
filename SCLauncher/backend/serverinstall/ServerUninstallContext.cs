using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall;

public class ServerUninstallContext(ServerUninstallParams p)
{
	public ServerUninstallParams Params { get; } = p;
}