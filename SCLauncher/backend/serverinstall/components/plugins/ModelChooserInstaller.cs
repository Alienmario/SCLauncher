using SCLauncher.backend.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components.plugins;

public class ModelChooserInstaller : BasePluginInstaller
{
	public ModelChooserInstaller(InstallHelper helper) : base(helper)
	{
		GithubOwner = "Alienmario";
		GithubRepo = "ModelChooser";
		PluginFileName = "ultimate_modelchooser.smx";
		Component = ServerInstallComponent.ModelChooser;
	}
}