using System.Collections.Generic;

namespace SCLauncher.model.config;

public partial class ServerConfigurationSDK2013 : ServerConfiguration
{
	public bool UseSteamNetworking { get; set; } = true;
	
	public override List<string> ToLaunchParams()
	{
		List<string> list = [];
		
		if (UseSteamNetworking)
			list.AddRange(["+sv_use_steam_networking", "1"]);

		list.AddRange(base.ToLaunchParams());
		return list;
	}
}