using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SCLauncher.model.serverinstall;

public partial class ServerInstallParams(string subfolder, int appId) : INotifyPropertyChanged
{

	[Obsolete("Design only")]
	public ServerInstallParams() : this("", -1)
	{
		Parts = new HashSet<ServerInstallPart>(Enum.GetValues<ServerInstallPart>());
	}

	public string Subfolder { get; set; } = subfolder;

	public int AppId { get; set; } = appId;

	public ServerInstallMethod? Method { get; set; }

	public string? Path { get; set; }

	public bool CreateSubfolder { get; set; } = true;

	public ISet<ServerInstallPart> Parts { get; set; } = new HashSet<ServerInstallPart>();

}