using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SCLauncher.model.config;

public enum AppType
{
	[Description("Black Mesa")]
	BlackMesa,
	[Description("Half-Life 2 Deathmatch")]
	HL2DM
}

public partial class AppProfile : INotifyPropertyChanged
{

	public required string Name { get; set; }

	public required AppType AppType { get; set; }

	public uint GameAppId { get; set; }

	public uint ServerAppId { get; set; }

	public required string GameInstallFolder { get; set; }

	public required string ServerInstallFolder { get; set; }

	public required string ModFolder { get; set; }

	public required IDictionary<PlatformID, string> GameExecutable { get; set; }

	public string? GamePath { get; set; }

	public string? ServerPath { get; set; }

	public DateTime CreatedAt { get; set; }

	public required ServerConfiguration ServerConfig { get; set; }

	public required ClientConfiguration ClientConfig { get; set; }

	public ServerConfiguration NewServerConfig()
	{
		ServerConfig = GetDefaultServerConfigProvider(AppType)();
		return ServerConfig;
	}

	public ClientConfiguration NewClientConfig()
	{
		ClientConfig = GetDefaultClientConfigProvider(AppType)();
		return ClientConfig;
	}

	public override string ToString()
	{
		return Name;
	}

	public static Func<ServerConfiguration> GetDefaultServerConfigProvider(AppType appType)
	{
		return () =>
		{
			return appType switch
			{
				AppType.BlackMesa => new ServerConfiguration
				{
					Teamplay = true,
					StartMap = "bm_c0a0a",
					CustomParams = [
						new CustomParam("+modelchooser_teambased", "0")
					]
				},
				AppType.HL2DM => new ServerConfigurationHl2dm
				{
					Teamplay = false,
					StartMap = "dm_lockdown",
					CustomParams = []
				},
				_ => new ServerConfiguration()
			};
		};
	}

	public static Func<ClientConfiguration> GetDefaultClientConfigProvider(AppType appType)
	{
		return () =>
		{
			return appType switch
			{
				AppType.BlackMesa => new ClientConfigurationBlackMesa(),
				AppType.HL2DM => new ClientConfiguration(),
				_ => new ClientConfiguration()
			};
		};
	}

	public static AppProfile CreateDefaultBlackMesa()
	{
		var profile = new AppProfile
		{
			Name = "Black Mesa",
			AppType = AppType.BlackMesa,
			GameAppId = 362890,
			ServerAppId = 346680,
			GameInstallFolder = "Black Mesa",
			ServerInstallFolder = "Black Mesa Dedicated Server",
			ModFolder = "bms",
			GameExecutable = new Dictionary<PlatformID, string>
			{
				{ PlatformID.Win32NT, "bms.exe" },
				{ PlatformID.Unix, "bms.sh" }
			},
			CreatedAt = DateTime.Now,
			ServerConfig = GetDefaultServerConfigProvider(AppType.BlackMesa)(),
			ClientConfig = GetDefaultClientConfigProvider(AppType.BlackMesa)()
		};
		return profile;
	}

	public static AppProfile CreateDefaultHL2DM()
	{
		var profile = new AppProfile
		{
			Name = "Half-Life 2 Deathmatch",
			AppType = AppType.HL2DM,
			GameAppId = 320,
			ServerAppId = 232370,
			GameInstallFolder = "Half-Life 2 Deathmatch",
			ServerInstallFolder = "Half-Life 2 Deathmatch Dedicated Server",
			ModFolder = "hl2mp",
			GameExecutable = new Dictionary<PlatformID, string>
			{
				{ PlatformID.Win32NT, "hl2mp_win64.exe" },
				{ PlatformID.Unix, "hl2mp.sh" }
			},
			CreatedAt = DateTime.Now,
			ServerConfig = GetDefaultServerConfigProvider(AppType.HL2DM)(),
			ClientConfig = GetDefaultClientConfigProvider(AppType.HL2DM)()
		};
		return profile;
	}
}
