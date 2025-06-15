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
		Path = @"C:\Program Files\Program Files\Program Files\Program Files\Program Files\Program Files\Server";
	}
}

public class DServerUninstallParams : ServerUninstallParams
{
	public DServerUninstallParams()
	{
		AppInfo = DAppInfo.Instance;
		Path = @"C:\Program Files\Program Files\Program Files\Program Files\Program Files\Program Files\Server";
	}
}

public class DServerConfiguration : ServerConfiguration
{
	public DServerConfiguration()
	{
		Teamplay = true;
		StartMap = "bm_c0a0a";
		CustomParams.Add(new CustomParam { Key = "key1", Value = "value1" });
		CustomParams.Add(new CustomParam { Key = "key2", Value = "value2" });
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
		GameExecutable = new Dictionary<PlatformID, string>(),
		DefaultServerConfigProvider = () => new DServerConfiguration()
	};
}