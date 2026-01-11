using System.Collections.Generic;

namespace SCLauncher.model.config;

public partial class ServerConfigurationHl2dm : ServerConfiguration
{

	public bool UseSteamNetworking { get; set; } = true;
	
	public override List<string> ToLaunchParams()
	{
		List<string> list = [];
		
		if (UseSteamNetworking)
			list.AddRange(["+sv_use_steam_networking", "1"]);

		list.AddRange(base.ToLaunchParams());
		
		// there is a weird quirk where the hostname will be ignored from the command line unless we add a wait
		// make sure this goes last
		if (Hostname != null)
			list.AddRange(["+wait", "+hostname", Hostname]);
		
		return list;
	}

}