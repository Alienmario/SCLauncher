using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SCLauncher.backend.util;
using SCLauncher.model;

namespace SCLauncher.backend.service;

public class BackendService
{
	public AppInfo ActiveApp => activeApp!;

	private AppInfo? activeApp;
	private readonly GlobalConfiguration globalConfig;
	private readonly PersistenceService persistence;
	private ServerConfiguration? serverConfig;

	public BackendService(GlobalConfiguration globalConfig, PersistenceService persistence)
	{
		this.globalConfig = globalConfig;
		this.persistence = persistence;
		Initialize();
	}

	private void Initialize()
	{
		persistence.Bind("config", globalConfig, JsonSourceGenerationContext.Default);
		
		activeApp = new AppInfo
		{
			GameAppId = 362890, ServerAppId = 346680,
			GameInstallFolder = "Black Mesa", ServerInstallFolder = "Black Mesa Dedicated Server",
			ModFolder = "bms",
			GameExecutable = new Dictionary<PlatformID, string>
			{
				{ PlatformID.Win32NT, "bms.exe" }, { PlatformID.Unix, "bms.sh" }
			},
			DefaultServerConfigProvider = () => new ServerConfiguration
			{
				Teamplay = true,
				StartMap = "bm_c0a0a"
			}
		};

		Task.Run(async () =>
		{
			var steamDir = SteamUtils.FindSteamInstallDir();
			if (steamDir != null)
			{
				globalConfig.SteamPath = steamDir;

				if (!Directory.Exists(globalConfig.GamePath))
					globalConfig.GamePath = await SteamUtils.FindAppPathAsync(steamDir, ActiveApp.GameAppId)
					                  ?? globalConfig.GamePath;

				if (!Directory.Exists(globalConfig.ServerPath))
					globalConfig.ServerPath = await SteamUtils.FindAppPathAsync(steamDir, ActiveApp.ServerAppId)
					                    ?? globalConfig.ServerPath;
			}
		}).Wait();
	}

	public ServerConfiguration GetServerConfig(bool reset = false)
	{
		if (serverConfig != null && !reset)
			return serverConfig;
		
		serverConfig = ActiveApp.NewServerConfig();
		persistence.Bind("server_" + ActiveApp.ModFolder, serverConfig, JsonSourceGenerationContext.Default, !reset);
		return serverConfig;
	}

}