using SCLauncher.backend.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components.plugins;

public class SourceCoopInstaller : BasePluginInstaller
{
	public SourceCoopInstaller(InstallHelper helper) : base(helper)
	{
		GithubOwner = "ampreeT";
		GithubRepo = "SourceCoop";
		PluginFileName = "srccoop.smx";
		Component = ServerInstallComponent.SourceCoop;
	}
}