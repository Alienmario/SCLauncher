using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.model.config;

namespace SCLauncher.backend.service;

public class BackendService
{
	public AppInfo ActiveApp => activeApp!;
	public string AppConfigKey(string name) => name + '_' + ActiveApp.ModFolder;

	private readonly GlobalConfiguration globalConfig;
	private readonly PersistenceService persistence;
	private AppInfo? activeApp;
	private ServerConfiguration? serverConfig;
	private ClientConfiguration? clientConfig;

	public BackendService(GlobalConfiguration globalConfig, PersistenceService persistence)
	{
		this.globalConfig = globalConfig;
		this.persistence = persistence;
		Initialize();
	}

	private void Initialize()
	{
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
			},
			DefaultClientConfigProvider = () => new ClientConfigurationBlackMesa()
		};

		persistence.Bind(AppConfigKey("config"), globalConfig, JsonSourceGenerationContext.Default);
		
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
		persistence.Bind(AppConfigKey("server"), serverConfig, JsonSourceGenerationContext.Default, !reset);
		return serverConfig;
	}
	
	public ClientConfiguration GetClientConfig(bool reset = false)
	{
		if (clientConfig != null && !reset)
			return clientConfig;
		
		clientConfig = ActiveApp.NewClientConfig();
		persistence.Bind(AppConfigKey("client"), clientConfig, JsonSourceGenerationContext.Default, !reset);
		return clientConfig;
	}

}