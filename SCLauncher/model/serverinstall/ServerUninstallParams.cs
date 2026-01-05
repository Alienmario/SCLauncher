using System.ComponentModel;
using SCLauncher.model.config;

namespace SCLauncher.model.serverinstall;

public partial class ServerUninstallParams : INotifyPropertyChanged
{
	
	/// App definition
	public required AppProfile Profile { get; init; }

	/// Path to the server root
	public string Path { get; set; } = string.Empty;

}