using System;
using System.Collections.Generic;
using SCLauncher.model;
using SCLauncher.model.serverinstall;

namespace SCLauncher.ui.design;

public class DServerInstallParams : ServerInstallParams
{
	public DServerInstallParams()
	{
		AppInfo = DAppInfo.Instance;
		Method = ServerInstallMethod.External;
		Path = @"C:\Program Files\Program Files\Program Files\Program Files\Program Files\Program Files";
	}
}

public class DAppInfo : AppInfo
{
	public static readonly DAppInfo Instance = new()
	{
		GameAppId = 362890,
		ServerAppId = 346680,
		GameInstallFolder = "Black Mesa",
		ServerInstallFolder = "Black Mesa Dedicated Server",
		ModFolder = "bms",
		GameExecutable = new Dictionary<PlatformID, string>()
	};
}