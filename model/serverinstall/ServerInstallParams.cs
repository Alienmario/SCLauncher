using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SCLauncher.model.serverinstall;

public partial class ServerInstallParams : INotifyPropertyChanged
{
	public ServerInstallParams()
	{
		Components = new HashSet<ServerInstallComponent>(Enum.GetValues<ServerInstallComponent>());
	}

	public ServerInstallMethod? Method { get; set; }

	public string? Path { get; set; }

	public bool CreateSubfolder { get; set; } = true;

	public required string Subfolder { get; set; }
	
	public required int AppId { get; set; }
	
	public ISet<ServerInstallComponent> Components { get; set; }

	public bool AllowUpgrades { get; set; } = true;

}