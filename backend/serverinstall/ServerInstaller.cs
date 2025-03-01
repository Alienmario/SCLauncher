using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall;

public static class ServerInstaller
{
	public static async IAsyncEnumerable<ServerInstallMessage> Run(ServerInstallParams installParams,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		yield return new ServerInstallMessage("Server installation started");
		for (int i = 0; i < 50; i++)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				yield return new ServerInstallMessage("Cancelled");
				break;
			}
			await Task.Delay(100, CancellationToken.None);
			yield return new ServerInstallMessage("New progress received: " + i);
		}
		// SteamUtils.InstallApp(backend.GetActiveApp().ServerId);
	}
}