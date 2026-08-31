using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SCLauncher.model.config;

namespace SCLauncher.model.definition;

public sealed record AppDefinition(
	ImmutableList<AppPreset> AvailablePresets,
	Func<AppPreset, AppProfile> ProfileFactory,
	Func<AppPreset, ServerConfiguration>? ServerConfigFactory = null,
	Func<AppPreset, ClientConfiguration>? ClientConfigFactory = null
)
{
	public ServerConfiguration NewServerConfig(AppPreset preset = default) =>
		ServerConfigFactory?.Invoke(preset) ?? new ServerConfiguration();

	public ClientConfiguration NewClientConfig(AppPreset preset = default) =>
		ClientConfigFactory?.Invoke(preset) ?? new ClientConfiguration();
}

public static class AppDefinitions
{
	public static AppDefinition Get(AppType type)
	{
		return Definitions[type];
	}

	private static readonly Dictionary<AppType, AppDefinition> Definitions = new()
	{
		[AppType.BlackMesa] = new(
			AvailablePresets: [AppPreset.Default, AppPreset.Cooperative],
			ProfileFactory: preset => new AppProfile(AppType.BlackMesa, preset)
			{
				GameAppId = 362890,
				ServerAppId = 346680,
				GameInstallFolder = "Black Mesa",
				ServerInstallFolder = "Black Mesa Dedicated Server",
				ModFolder = "bms",
				GameExecutable = Exe("bms.exe", "bms.sh")
			},
			ServerConfigFactory: preset => preset switch
			{
				AppPreset.Cooperative => new ServerConfiguration
				{
					Teamplay = true,
					StartMap = "bm_c0a0a",
					ServerCfgFile = "coop.cfg",
					CustomParams = [
						new CustomParam("+modelchooser_teambased", "0")
					]
				},
				_ => new ServerConfiguration
				{
					Teamplay = false,
					StartMap = "dm_crossfire"
				}
			},
			ClientConfigFactory: _ => new ClientConfigurationBlackMesa()
		),

		[AppType.HL2DM] = new(
			AvailablePresets: [AppPreset.Default, AppPreset.Cooperative],
			ProfileFactory: preset => new AppProfile(AppType.HL2DM, preset)
			{
				GameAppId = 320,
				ServerAppId = 232370,
				GameInstallFolder = "Half-Life 2 Deathmatch",
				ServerInstallFolder = "Half-Life 2 Deathmatch Dedicated Server",
				ModFolder = "hl2mp",
				GameExecutable = Exe("hl2mp_win64.exe", "hl2mp.sh")
			},
			ServerConfigFactory: preset => preset switch
			{
				AppPreset.Cooperative => new ServerConfigurationHl2dm
				{
					Teamplay = true,
					StartMap = "dm_lockdown",
					ServerCfgFile = "coop.cfg",
					CustomParams = [
						new CustomParam("+modelchooser_teambased", "0")
					]
				},
				_ => new ServerConfigurationHl2dm
				{
					Teamplay = false,
					StartMap = "dm_lockdown"
				}
			}
		),

		[AppType.TF2] = new(
			AvailablePresets: [AppPreset.Default],
			ProfileFactory: preset => new AppProfile(AppType.TF2, preset)
			{
				GameAppId = 440,
				ServerAppId = 232250,
				GameInstallFolder = "Team Fortress 2",
				ServerInstallFolder = "Team Fortress 2 Dedicated Server",
				ModFolder = "tf",
				GameExecutable = Exe("tf_win64.exe", "tf.sh")
			},
			ServerConfigFactory: _ => new ServerConfiguration
			{
				StartMap = "cp_dustbowl"
			}
		),

		[AppType.CSS] = new(
			AvailablePresets: [AppPreset.Default],
			ProfileFactory: preset => new AppProfile(AppType.CSS, preset)
			{
				GameAppId = 240,
				ServerAppId = 232330,
				GameInstallFolder = "Counter-Strike Source",
				ServerInstallFolder = "Counter-Strike Source Dedicated Server",
				ModFolder = "cstrike",
				GameExecutable = Exe("cstrike_win64.exe", "cstrike.sh")
			},
			ServerConfigFactory: _ => new ServerConfiguration
			{
				StartMap = "de_dust2"
			}
		),

		[AppType.DODS] = new(
			AvailablePresets: [AppPreset.Default],
			ProfileFactory: preset => new AppProfile(AppType.DODS, preset)
			{
				GameAppId = 300,
				ServerAppId = 232290,
				GameInstallFolder = "Day of Defeat Source",
				ServerInstallFolder = "Day of Defeat Source Dedicated Server",
				ModFolder = "dod",
				GameExecutable = Exe("dod_win64.exe", "dod.sh")
			},
			ServerConfigFactory: _ => new ServerConfiguration
			{
				StartMap = "dod_donner"
			}
		),

		[AppType.GMOD] = new(
			AvailablePresets: [AppPreset.Default],
			ProfileFactory: preset => new AppProfile(AppType.GMOD, preset)
			{
				GameAppId = 4000,
				ServerAppId = 4020,
				GameInstallFolder = "GarrysMod",
				ServerInstallFolder = "GarrysModDS",
				ModFolder = "garrysmod",
				GameExecutable = Exe("gmod.exe", "hl2.sh")
			},
			ServerConfigFactory: _ => new ServerConfiguration
			{
				StartMap = "gm_construct"
			}
		),

		[AppType.Left4Dead] = new(
			AvailablePresets: [AppPreset.Default],
			ProfileFactory: preset => new AppProfile(AppType.Left4Dead, preset)
			{
				GameAppId = 500,
				ServerAppId = 222840,
				GameInstallFolder = "left 4 dead",
				ServerInstallFolder = "Left 4 Dead Dedicated Server",
				ModFolder = "left4dead",
				GameExecutable = Exe("left4dead.exe")
			},
			ServerConfigFactory: _ => new ServerConfiguration
			{
				StartMap = "l4d_hospital01_apartment"
			}
		),

		[AppType.Left4Dead2] = new(
			AvailablePresets: [AppPreset.Default],
			ProfileFactory: preset => new AppProfile(AppType.Left4Dead2, preset)
			{
				GameAppId = 550,
				ServerAppId = 222860,
				GameInstallFolder = "Left 4 Dead 2",
				ServerInstallFolder = "Left 4 Dead 2 Dedicated Server",
				ModFolder = "left4dead2",
				GameExecutable = Exe("left4dead2.exe", "hl2.sh")
			},
			ServerConfigFactory: _ => new ServerConfiguration
			{
				StartMap = "c1m1_hotel"
			}
		),

		[AppType.Insurgency] = new(
			AvailablePresets: [AppPreset.Default],
			ProfileFactory: preset => new AppProfile(AppType.Insurgency, preset)
			{
				GameAppId = 222880,
				ServerAppId = 237410,
				GameInstallFolder = "insurgency2",
				ServerInstallFolder = "Insurgency Dedicated Server",
				ModFolder = "insurgency",
				GameExecutable = Exe("insurgency.exe", "insurgency.sh")
			},
			ServerConfigFactory: _ => new ServerConfiguration
			{
				StartMap = "market"
			}
		),

		[AppType.Synergy] = new(
			AvailablePresets: [AppPreset.Default],
			ProfileFactory: preset => new AppProfile(AppType.Synergy, preset)
			{
				GameAppId = 17520,
				ServerAppId = 17525,
				GameInstallFolder = "Synergy",
				ServerInstallFolder = "Synergy Dedicated Server",
				ModFolder = "synergy",
				GameExecutable = Exe("synergy.exe", "hl2.sh")
			},
			ServerConfigFactory: _ => new ServerConfiguration
			{
				StartMap = "syn_deadsimple"
			}
		),

		[AppType.NMRIH] = new(
			AvailablePresets: [AppPreset.Default],
			ProfileFactory: preset => new AppProfile(AppType.NMRIH, preset)
			{
				GameAppId = 224260,
				ServerAppId = 317670,
				GameInstallFolder = "nmrih",
				ServerInstallFolder = "No More Room in Hell Dedicated Server",
				ModFolder = "nmrih",
				GameExecutable = Exe("nmrih.exe", "hl2.sh")
			},
			ServerConfigFactory: _ => new ServerConfiguration
			{
				StartMap = "nmo_broadway"
			}
		),

		[AppType.SDK2013] = new(
			AvailablePresets: [AppPreset.Default],
			ProfileFactory: preset => new AppProfile(AppType.SDK2013, preset)
			{
				GameAppId = 243750,
				ServerAppId = 244310,
				GameInstallFolder = "Source SDK Base 2013 Multiplayer",
				ServerInstallFolder = "Source SDK Base 2013 Dedicated Server",
				ModFolder = "hl2mp",
				GameExecutable = Exe("hl2.exe", "hl2.sh")
			},
			ServerConfigFactory: _ => new ServerConfiguration()
		),
	};

	private static Dictionary<PlatformID, string> Exe(string winExe, string? unixExe = null)
	{
		var dict = new Dictionary<PlatformID, string> { [PlatformID.Win32NT] = winExe };
		if (unixExe != null)
		{
			dict[PlatformID.Unix] = unixExe;
		}
		return dict;
	}
}
