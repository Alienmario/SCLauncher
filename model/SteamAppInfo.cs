namespace SCLauncher.model;

public class SteamAppInfo
{
	public SteamAppInfo(int id, int serverId, string gameFolder, string serverFolder)
	{
		Id = id;
		ServerId = serverId;
		GameFolder = gameFolder;
		ServerFolder = serverFolder;
	}

	public int Id { get;}
	
	public int ServerId { get; }
	
	public string GameFolder { get; }
	
	public string ServerFolder { get; }

}