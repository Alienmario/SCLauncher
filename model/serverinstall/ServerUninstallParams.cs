using System.ComponentModel;

namespace SCLauncher.model.serverinstall;

public partial class ServerUninstallParams : INotifyPropertyChanged
{
	
	/// App definition
	public required AppInfo AppInfo { get; init; }
	
	/// Path to the server root
	public string Path { get; set; } = string.Empty;

}