using System.Collections.Generic;
using System.IO;
using SCLauncher.backend.install;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall;

public class ServerInstallContext
{
	public ServerInstallContext(ServerInstallParams p)
	{
		Params = p;
		if (p.Method == ServerInstallMethod.External)
		{
			InstallPath =
				p.CreateSubfolder
					? Path.Join(p.Path, p.Profile.ServerInstallFolder)
					: p.Path ?? p.Profile.ServerInstallFolder;
		}
	}

	public ServerInstallParams Params { get; }

	public readonly Dictionary<ServerInstallComponent, ComponentInfo> ComponentInfos = new();

	/// Full install path ( server root )
	/// <exception cref="UnsetInstallPathException">Throws if base path has not been set</exception>
	public string InstallPath
	{
		get
		{
			if (ComponentInfos.TryGetValue(ServerInstallComponent.Server, out ComponentInfo? info) && info.Path != null)
				return info.Path;
			return field ?? throw new UnsetInstallPathException();
		}
	}

	/// Full path to the default game-mod folder under the server root
	/// <exception cref="UnsetInstallPathException">Throws if base path has not been set</exception>
	public string GameModPath => Path.Join(InstallPath, Params.Profile.ModFolder);

	/// Full addons path
	/// <exception cref="UnsetInstallPathException">Throws if base path has not been set</exception>
	public string AddonsPath => Path.Join(InstallPath, Params.Profile.ModFolder, "addons");
}