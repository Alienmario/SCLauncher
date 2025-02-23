using System.Collections.Generic;
using SCLauncher.backend.serverinstall;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.service;

public class ServerInstallService(BackendService backend)
{

	public ServerInstallParams NewInstallParams()
	{
		return new ServerInstallParams(
			backend.GetActiveApp().ServerFolder,
			backend.GetActiveApp().ServerId);
	}
	
	public IAsyncEnumerable<ServerInstallMessage> RunInstaller(ServerInstallParams installParams)
	{
		return ServerInstaller.Run(installParams);
	}
	
}