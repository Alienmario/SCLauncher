using System.Collections.Generic;

namespace SCLauncher.model.config;

public partial class ServerConfigurationHL2DM : ServerConfigurationSDK2013
{
	
	public override List<string> ToLaunchParams()
	{
		List<string> list = [.. base.ToLaunchParams()];
		
		// there is a weird quirk where the hostname will be ignored from the command line unless we add a wait
		// make sure this goes last
		if (Hostname != null)
			list.AddRange(["+wait", "+hostname", Hostname]);
		
		return list;
	}

}