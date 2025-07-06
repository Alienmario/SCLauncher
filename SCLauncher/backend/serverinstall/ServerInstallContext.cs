using System.Collections.Generic;
using System.IO;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall;

public class ServerInstallContext
{
	public ServerInstallContext(ServerInstallParams p)
	{
		Params = p;
		installDir =
			p.CreateSubfolder
				? Path.Join(p.Path, p.AppInfo.ServerInstallFolder)
				: p.Path ?? p.AppInfo.ServerInstallFolder;
	}

	public ServerInstallParams Params { get; }

	public readonly IDictionary<ServerInstallComponent, ComponentInfo> ComponentInfos
		= new Dictionary<ServerInstallComponent, ComponentInfo>();

	private readonly string installDir;

	/// Full install path ( server root )
	public string InstallDir
	{
		get
		{
			if (ComponentInfos.TryGetValue(ServerInstallComponent.Server, out ComponentInfo? info) && info.Path != null)
				return info.Path;
			return installDir;
		}
	}

	/// Full path to the default game/mod folder under the server root
	public string GameModDir => Path.Join(InstallDir, Params.AppInfo.ModFolder);

	/// Full addons path
	public string AddonsDir => Path.Join(InstallDir, Params.AppInfo.ModFolder, "addons");
}