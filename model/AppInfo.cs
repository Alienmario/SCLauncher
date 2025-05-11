namespace SCLauncher.model;

public class AppInfo
{
	public AppInfo(int gameAppId, int serverAppId, string gameInstallFolder, string serverInstallFolder, string modFolder)
	{
		GameAppId = gameAppId;
		ServerAppId = serverAppId;
		GameInstallFolder = gameInstallFolder;
		ServerInstallFolder = serverInstallFolder;
		ModFolder = modFolder;
	}

	public int GameAppId { get; }
	
	public int ServerAppId { get; }
	
	public string GameInstallFolder { get; }
	
	public string ServerInstallFolder { get; }
	
	public string ModFolder { get; }

}