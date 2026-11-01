using System.ComponentModel;
using SCLauncher.Shared.Model.Config;

namespace SCLauncher.Shared.Model.ServerInstall;

public partial class ServerUninstallParams : INotifyPropertyChanged
{
	
	/// App definition
	public required AppProfile Profile { get; init; }

	/// Path to the server root
	public string Path { get; set; } = string.Empty;

}