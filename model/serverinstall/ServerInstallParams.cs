using System.Collections.Generic;
using System.ComponentModel;

namespace SCLauncher.model.serverinstall;

public partial class ServerInstallParams : INotifyPropertyChanged
{

	public ServerInstallMethod? Method { get; set; }
	
	public string? Path { get; set; }

	public bool CreateSubfolder { get; set; } = true;

	public ISet<ServerInstallPart> Parts { get; set; } = new HashSet<ServerInstallPart>();

}