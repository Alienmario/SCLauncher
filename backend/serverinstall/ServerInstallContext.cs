using System;
using System.IO;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall;

public class ServerInstallContext
{
	public ServerInstallContext(ServerInstallParams p)
	{
		Params = p;
		InstallDir = (p.CreateSubfolder ? Path.Join(p.Path, p.Subfolder) : p.Path)
						?? throw new ArgumentException("Valid path required");
	}

	public ServerInstallParams Params { get; init; }
	
	public string InstallDir { get; init; }
	
}