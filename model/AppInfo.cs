using System;
using System.Collections.Generic;

namespace SCLauncher.model;

public class AppInfo
{
	
	public required int GameAppId { get; init; }
	
	public required int ServerAppId { get; init; }
	
	public required string GameInstallFolder { get; init; }
	
	public required string ServerInstallFolder { get; init; }
	
	public required string ModFolder { get; init; }
	
	public required IDictionary<PlatformID, string> GameExecutable { get; init; }

	public Func<ServerConfiguration> DefaultServerConfigProvider { init; private get; } = () => new ServerConfiguration();

	public ServerConfiguration NewServerConfig() => DefaultServerConfigProvider.Invoke();
	
}