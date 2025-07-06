using SCLauncher.backend.install;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall;

public interface IServerComponentInstaller<TComponentInfo> : IComponentInstaller<TComponentInfo, ServerInstallContext, ServerUninstallContext> 
					where TComponentInfo : ComponentInfo
{

	public ServerInstallComponent ComponentType { get; }
	
}