using Gameloop.Vdf.Linq;

namespace SCLauncher.backend.steam;

public class SteamLibrary(VObject vObject)
{
	
	public VObject VObject => vObject;

	public string? Path => vObject["path"]?.Value<string>();
	
}