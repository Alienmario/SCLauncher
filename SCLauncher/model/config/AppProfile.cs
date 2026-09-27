using System;
using System.Collections.Generic;
using System.ComponentModel;
using SCLauncher.model.definition;

namespace SCLauncher.model.config;

public partial class AppProfile : INotifyPropertyChanged
{
	public AppProfile(AppType appType, AppPreset appPreset = default)
	{
		AppType = appType;
		AppPreset = appPreset;
		Name = appType.GetDescription();
		ServerConfig = ResetServerConfig();
		ClientConfig = ResetClientConfig();
		CreatedAt = DateTime.Now;
	}

	public string Name { get; set; }

	public AppType AppType { get; set; }

	public AppPreset AppPreset { get; set; }

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

	public ServerConfiguration ResetServerConfig()
	{
		ServerConfig = AppDefinitions.Get(AppType).NewServerConfig(AppPreset);
		return ServerConfig;
	}

	public ClientConfiguration ResetClientConfig()
	{
		ClientConfig = AppDefinitions.Get(AppType).NewClientConfig(AppPreset);
		return ClientConfig;
	}

	public override string ToString()
	{
		return Name;
	}

	public static AppProfile Create(AppType type, AppPreset preset = default)
	{
		return AppDefinitions.Get(type).ProfileFactory(preset);
	}
}
