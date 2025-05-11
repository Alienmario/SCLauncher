using System.IO;
using Gameloop.Vdf.Linq;

namespace SCLauncher.backend.steam;

public class SteamAppManifest(VObject vObject, SteamLibrary library)
{
	public VObject VObject => vObject;
	public SteamLibrary Library => library;

	public uint? AppId => vObject["appid"]?.Value<uint>();
	public uint? Universe => vObject["universe"]?.Value<uint>();
	public string? LauncherPath => vObject["LauncherPath"]?.Value<string>();
	public string? Name => vObject["name"]?.Value<string>();
	public string? InstallDir => vObject["installdir"]?.Value<string>();
	public uint? BuildId => vObject["buildid"]?.Value<uint>();
	
	public SteamAppState StateFlags
	{
		get
		{
			VToken? flagsToken = vObject["StateFlags"];
			return flagsToken == null ? SteamAppState.StateInvalid : (SteamAppState)flagsToken.Value<int>();
		}
	}

	public string? GetAbsInstallPath()
	{
		string? installDir = InstallDir;
		string? libraryPath = library.Path;
		if (libraryPath == null || installDir == null)
			return null;
		
		return Path.Combine(libraryPath, "steamapps", "common", installDir);
	}

}