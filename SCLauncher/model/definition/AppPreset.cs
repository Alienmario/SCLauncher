using System.ComponentModel;
using SCLauncher.model.serverinstall;

namespace SCLauncher.model.definition;

/// <seealso cref="AppDefinitions"/>
/// <seealso cref="ServerInstallComponent"/>
public enum AppPreset
{
	[Description("Default")]
	Default,
	[Description("Cooperative")]
	Cooperative
}
