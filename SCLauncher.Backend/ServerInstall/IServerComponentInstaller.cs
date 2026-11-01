using SCLauncher.Backend.Install;
using SCLauncher.Shared.Model.Install;
using SCLauncher.Shared.Model.ServerInstall;

namespace SCLauncher.Backend.ServerInstall;

internal interface IServerComponentInstaller<TComponentInfo> : IComponentInstaller<TComponentInfo, ServerInstallContext, ServerUninstallContext> 
					where TComponentInfo : ComponentInfo
{

	public ServerInstallComponent Component { get; }
	
}