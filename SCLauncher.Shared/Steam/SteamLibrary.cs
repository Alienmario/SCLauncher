using Gameloop.Vdf.Linq;

namespace SCLauncher.Shared.Steam;

public class SteamLibrary(VObject vObject)
{
	
	public VObject VObject => vObject;

	public string? Path => vObject["path"]?.Value<string>();
	
}