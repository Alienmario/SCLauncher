using System.ComponentModel;
using SCLauncher.Shared.Model.ServerInstall;

namespace SCLauncher.Shared.Model.Definition;

/// <seealso cref="AppDefinitions"/>
/// <seealso cref="ServerInstallComponent"/>
public enum AppPreset
{
	[Description("Default")]
	Default,
	[Description("Cooperative")]
	Cooperative
}
