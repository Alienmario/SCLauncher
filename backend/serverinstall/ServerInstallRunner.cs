using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall;

public class ServerInstallRunner(IEnumerable<IServerComponentInstaller<ComponentInfo>> installers)
{
	public async IAsyncEnumerable<StatusMessage> Run(ServerInstallParams installParams,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		ServerInstallContext ctx = new ServerInstallContext(installParams);
		
		yield return new StatusMessage("Installation process started");

		foreach (var component in Enum.GetValues<ServerInstallComponent>())
		{
			if (!ctx.Params.Components.Contains(component))
				continue;

			var installer = installers.FirstOrDefault(i => i.Type == component);
			if (installer == null)
				continue;
			
			if (cancellationToken.IsCancellationRequested)
				yield break;

			ComponentInfo? info = await installer.GatherInfo(ctx);
			
			if (cancellationToken.IsCancellationRequested)
				yield break;
			
			if (!await installer.ShouldInstall(ctx, info))
				continue;
			
			if (cancellationToken.IsCancellationRequested)
				yield break;
			
			yield return new StatusMessage($"Installing component <{component}>");

			await foreach (var message in installer.Install(ctx, cancellationToken))
			{
				yield return message;
				
				if (message.Status == MessageStatus.Error)
					yield break;
			}
			
			yield return new StatusMessage($"Component <{component}> installed");
		}

		yield return new StatusMessage("Installation finished successfully", MessageStatus.Success);
	}
	
}