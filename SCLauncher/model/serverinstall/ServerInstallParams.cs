using System.Collections.Generic;
using System.ComponentModel;

namespace SCLauncher.model.serverinstall;

public partial class ServerInstallParams : INotifyPropertyChanged
{
	/// App definition
	public required AppInfo AppInfo { get; init; }
	
	/// Steam, External
	public ServerInstallMethod? Method { get; set; }

	/// Only used with external method
	public string? Path { get; set; }

	/// Create a subfolder under Path using AppInfo?
	public bool CreateSubfolder { get; set; } = true;

	/// Components to install
	public ISet<ServerInstallComponent> Components { get; set; } = new HashSet<ServerInstallComponent>();
}