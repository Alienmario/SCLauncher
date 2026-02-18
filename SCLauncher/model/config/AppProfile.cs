using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SCLauncher.model.config;

public enum AppType
{
	[Description("Black Mesa")]
	BlackMesa,
	[Description("Black Mesa (Cooperative)")]
	BlackMesaCOOP,
	[Description("Half-Life 2 Deathmatch")]
	HL2DM,
	[Description("Half-Life 2 Deathmatch (Cooperative)")]
	HL2DMCOOP,
	[Description("Team Fortress 2")]
	TF2,
	[Description("Counter-Strike: Source")]
	CSS,
	[Description("Day of Defeat: Source")]
	DODS,
	[Description("Garry's Mod")]
	GMOD,
	[Description("Left 4 Dead")]
	Left4Dead,
	[Description("Left 4 Dead 2")]
	Left4Dead2,
	[Description("Insurgency")]
	Insurgency,
	[Description("Synergy")]
	Synergy,
	[Description("No More Room in Hell")]
	NMRIH,
	[Description("Source SDK Base 2013")]
	SDK2013,
}

public partial class AppProfile : INotifyPropertyChanged
{
	public AppProfile(AppType appType)
	{
		AppType = appType;
		Name = appType.GetDescription();
		ServerConfig = NewServerConfig();
		ClientConfig = NewClientConfig();
		CreatedAt = DateTime.Now;
	}

	public string Name { get; set; }

	public AppType AppType { get; set; }

	public uint GameAppId { get; set; }

	public uint ServerAppId { get; set; }

	public required string GameInstallFolder { get; set; }

	public required string ServerInstallFolder { get; set; }

	public required string ModFolder { get; set; }

	public required IDictionary<PlatformID, string> GameExecutable { get; set; }

	public string? GamePath { get; set; }

	public string? ServerPath { get; set; }

	public DateTime CreatedAt { get; set; }

	public ServerConfiguration ServerConfig { get; set; }

	public ClientConfiguration ClientConfig { get; set; }

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
					Teamplay = false,
					StartMap = "dm_crossfire"
				},
				AppType.BlackMesaCOOP => new ServerConfiguration
				{
					Teamplay = true,
					StartMap = "bm_c0a0a",
					ServerCfgFile = "coop.cfg",
					CustomParams = [
						new CustomParam("+modelchooser_teambased", "0")
					]
				},
				AppType.HL2DM => new ServerConfigurationHl2dm
				{
					Teamplay = false,
					StartMap = "dm_lockdown"
				},
				AppType.HL2DMCOOP => new ServerConfigurationHl2dm
				{
					Teamplay = true,
					StartMap = "dm_lockdown",
					ServerCfgFile = "coop.cfg",
					CustomParams = [
						new CustomParam("+modelchooser_teambased", "0")
					]
				},
				AppType.TF2 => new ServerConfiguration
				{
					StartMap = "cp_dustbowl"
				},
				AppType.CSS => new ServerConfiguration
				{
					StartMap = "de_dust2"
				},
				AppType.DODS => new ServerConfiguration
				{
					StartMap = "dod_donner"
				},
				AppType.GMOD => new ServerConfiguration
				{
					StartMap = "gm_construct"
				},
				AppType.Left4Dead => new ServerConfiguration
				{
					StartMap = "l4d_hospital01_apartment"
				},
				AppType.Left4Dead2 => new ServerConfiguration
				{
					StartMap = "c1m1_hotel"
				},
				AppType.Insurgency => new ServerConfiguration
				{
					StartMap = "market"
				},
				AppType.Synergy => new ServerConfiguration
				{
					StartMap = "syn_deadsimple"
				},
				AppType.NMRIH => new ServerConfiguration
				{
					StartMap = "nmo_broadway"
				},
				AppType.SDK2013 => new ServerConfiguration
				{
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
				AppType.BlackMesa or AppType.BlackMesaCOOP => new ClientConfigurationBlackMesa(),
				AppType.HL2DM or AppType.HL2DMCOOP => new ClientConfiguration(),
				_ => new ClientConfiguration()
			};
		};
	}

	public static AppProfile Create(AppType appType)
	{
		return appType switch
		{
			AppType.BlackMesa or AppType.BlackMesaCOOP => new AppProfile(appType)
			{
				GameAppId = 362890,
				ServerAppId = 346680,
				GameInstallFolder = "Black Mesa",
				ServerInstallFolder = "Black Mesa Dedicated Server",
				ModFolder = "bms",
				GameExecutable = new Dictionary<PlatformID, string>
				{
					{ PlatformID.Win32NT, "bms.exe" },
					{ PlatformID.Unix, "bms.sh" }
				}
			},
			AppType.HL2DM or AppType.HL2DMCOOP => new AppProfile(appType)
			{
				GameAppId = 320,
				ServerAppId = 232370,
				GameInstallFolder = "Half-Life 2 Deathmatch",
				ServerInstallFolder = "Half-Life 2 Deathmatch Dedicated Server",
				ModFolder = "hl2mp",
				GameExecutable = new Dictionary<PlatformID, string>
				{
					{ PlatformID.Win32NT, "hl2mp_win64.exe" },
					{ PlatformID.Unix, "hl2mp.sh" }
				}
			},
			AppType.TF2 => new AppProfile(appType)
			{
				GameAppId = 440,
				ServerAppId = 232250,
				GameInstallFolder = "Team Fortress 2",
				ServerInstallFolder = "Team Fortress 2 Dedicated Server",
				ModFolder = "tf",
				GameExecutable = new Dictionary<PlatformID, string>
				{
					{ PlatformID.Win32NT, "tf_win64.exe" },
					{ PlatformID.Unix, "tf.sh" }
				}
			},
			AppType.CSS => new AppProfile(appType)
			{
				GameAppId = 240,
				ServerAppId = 232330,
				GameInstallFolder = "Counter-Strike Source",
				ServerInstallFolder = "Counter-Strike Source Dedicated Server",
				ModFolder = "cstrike",
				GameExecutable = new Dictionary<PlatformID, string>
				{
					{ PlatformID.Win32NT, "cstrike_win64.exe" },
					{ PlatformID.Unix, "cstrike.sh" }
				}
			},
			AppType.DODS => new AppProfile(appType)
			{
				GameAppId = 300,
				ServerAppId = 232290,
				GameInstallFolder = "Day of Defeat Source",
				ServerInstallFolder = "Day of Defeat Source Dedicated Server",
				ModFolder = "dod",
				GameExecutable = new Dictionary<PlatformID, string>
				{
					{ PlatformID.Win32NT, "dod_win64.exe" },
					{ PlatformID.Unix, "dod.sh" }
				}
			},
			AppType.GMOD => new AppProfile(appType)
			{
				GameAppId = 4000,
				ServerAppId = 4020,
				GameInstallFolder = "GarrysMod",
				ServerInstallFolder = "GarrysModDS",
				ModFolder = "garrysmod",
				GameExecutable = new Dictionary<PlatformID, string>
				{
					{ PlatformID.Win32NT, "gmod.exe" },
					{ PlatformID.Unix, "hl2.sh" }
				}
			},
			AppType.Left4Dead => new AppProfile(appType)
			{
				GameAppId = 500,
				ServerAppId = 222840,
				GameInstallFolder = "left 4 dead",
				ServerInstallFolder = "Left 4 Dead Dedicated Server",
				ModFolder = "left4dead",
				GameExecutable = new Dictionary<PlatformID, string>
				{
					{ PlatformID.Win32NT, "left4dead.exe" }
				}
			},
			AppType.Left4Dead2 => new AppProfile(appType)
			{
				GameAppId = 550,
				ServerAppId = 222860,
				GameInstallFolder = "Left 4 Dead 2",
				ServerInstallFolder = "Left 4 Dead 2 Dedicated Server",
				ModFolder = "left4dead2",
				GameExecutable = new Dictionary<PlatformID, string>
				{
					{ PlatformID.Win32NT, "left4dead2.exe" },
					{ PlatformID.Unix, "hl2.sh" }
				}
			},
			AppType.Insurgency => new AppProfile(appType)
			{
				GameAppId = 222880,
				ServerAppId = 237410,
				GameInstallFolder = "insurgency2",
				ServerInstallFolder = "Insurgency Dedicated Server",
				ModFolder = "insurgency",
				GameExecutable = new Dictionary<PlatformID, string>
				{
					{ PlatformID.Win32NT, "insurgency.exe" },
					{ PlatformID.Unix, "insurgency.sh" }
				}
			},
			AppType.Synergy => new AppProfile(appType)
			{
				GameAppId = 17520,
				ServerAppId = 17525,
				GameInstallFolder = "Synergy",
				ServerInstallFolder = "Synergy Dedicated Server",
				ModFolder = "synergy",
				GameExecutable = new Dictionary<PlatformID, string>
				{
					{ PlatformID.Win32NT, "synergy.exe" },
					{ PlatformID.Unix, "hl2.sh" }
				}
			},
			AppType.NMRIH => new AppProfile(appType)
			{
				GameAppId = 224260,
				ServerAppId = 317670,
				GameInstallFolder = "nmrih",
				ServerInstallFolder = "No More Room in Hell Dedicated Server",
				ModFolder = "nmrih",
				GameExecutable = new Dictionary<PlatformID, string>
				{
					{ PlatformID.Win32NT, "nmrih.exe" },
					{ PlatformID.Unix, "hl2.sh" }
				}
			},
			AppType.SDK2013 => new AppProfile(appType)
			{
				GameAppId = 243750,
				ServerAppId = 244310,
				GameInstallFolder = "Source SDK Base 2013 Multiplayer",
				ServerInstallFolder = "Source SDK Base 2013 Dedicated Server",
				ModFolder = "hl2mp",
				GameExecutable = new Dictionary<PlatformID, string>
				{
					{ PlatformID.Win32NT, "hl2.exe" },
					{ PlatformID.Unix, "hl2.sh" }
				}
			},
			_ => throw new ArgumentOutOfRangeException(nameof(appType), appType, null)
		};
	}

}
