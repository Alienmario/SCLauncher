using System;
using System.Collections.Generic;
using SCLauncher.model;
using SCLauncher.model.config;
using SCLauncher.model.serverbrowser;
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

public record DServer : Server
{
	public static readonly DServer Instance = new()
	{
		IP = "123.456.789",
		Port = 27015,
		Name = "Server name",
		GameAppId = 123,
		GameModDir = "bms",
		GameDescription = "Black Mesa",
		NumPlayers = 12,
		MaxPlayers = 32,
		NumBots = 3,
		Map = "bm_dm",
		Type = "Dedicated",
		VAC = true,
		Password = true,
		Version = "1200",
		Keywords = "val1,val2,val3",
		SteamId = 123456789,
		Environment = "Linux",
		Ping = TimeSpan.FromMilliseconds(123),
		Players =
		[
			new ServerPlayer { Name = "Player 1", Score = 1, Duration = TimeSpan.Parse("00:00:10") },
			new ServerPlayer { Name = "Player 2", Score = 1000, Duration = TimeSpan.Parse("01:48:01") },
			new ServerPlayer { Name = "Player 3", Score = 1, Duration = TimeSpan.Parse("00:00:10") },
			new ServerPlayer { Name = "Player 4", Score = 1000, Duration = TimeSpan.Parse("01:48:01") }
		],
		Rules =
		[
			new ServerRule { Name = "Rule 1", Value = "Value 1" },
			new ServerRule { Name = "Rule 2", Value = "Value 2" }
		]
	};
}