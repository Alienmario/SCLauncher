using SCLauncher.Shared.Model.ServerInstall;
using SCLauncher.Shared.Service;

namespace SCLauncher.Backend.ServerInstall.Components.Plugins;

internal class ModelChooserInstaller : SourceModPluginInstaller
{
	public ModelChooserInstaller(IInstallHelperService helper) : base(helper)
	{
		GithubOwner = "Alienmario";
		GithubRepo = "ModelChooser";
		PluginFileName = "ultimate_modelchooser.smx";
		Component = ServerInstallComponent.ModelChooser;
	}
}