using SCLauncher.Shared.Model.ServerInstall;
using SCLauncher.Shared.Service;

namespace SCLauncher.Backend.ServerInstall.Components.Plugins;

internal class SourceCoopInstaller : SourceModPluginInstaller
{
	public SourceCoopInstaller(IInstallHelperService helper) : base(helper)
	{
		GithubOwner = "ampreeT";
		GithubRepo = "SourceCoop";
		PluginFileName = "srccoop.smx";
		Component = ServerInstallComponent.SourceCoop;
	}
}