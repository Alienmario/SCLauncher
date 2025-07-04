using System;
using System.Collections.Generic;
using SCLauncher.model.config;

namespace SCLauncher.model;

public class AppInfo
{
	
	public required uint GameAppId { get; init; }
	
	public required uint ServerAppId { get; init; }
	
	public required string GameInstallFolder { get; init; }
	
	public required string ServerInstallFolder { get; init; }
	
	public required string ModFolder { get; init; }
	
	public required IDictionary<PlatformID, string> GameExecutable { get; init; }

	public Func<ServerConfiguration> DefaultServerConfigProvider { init; private get; } = () => new ServerConfiguration();

	public ServerConfiguration NewServerConfig() => DefaultServerConfigProvider();

	public Func<ClientConfiguration> DefaultClientConfigProvider { init; private get; } = () => new ClientConfiguration();

	public ClientConfiguration NewClientConfig() => DefaultClientConfigProvider();
	
}