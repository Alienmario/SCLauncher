using System.Collections.Generic;
using System.ComponentModel;
using SCLauncher.model.config;

namespace SCLauncher.model.serverinstall;

public partial class ServerInstallParams : INotifyPropertyChanged
{
	/// Current app context
	public required AppProfile Profile { get; init; }

	/// Steam, Standalone
	public ServerInstallMethod? Method { get; set; }

	/// Only used with standalone method
	public string? Path { get; set; }

	/// Create a subfolder under Path using AppInfo?
	public bool CreateSubfolder { get; set; } = true;

	/// Components to install
	public IDictionary<ServerInstallComponent, ServerInstallComponentParams> Components { get; set; } =
		new Dictionary<ServerInstallComponent, ServerInstallComponentParams>();
}

public class ServerInstallComponentParams
{
	/// Optional specific version (tag or branch) to install. If null, latest stable is installed.
	public string? Version { get; set; }
}
