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
	public async IAsyncEnumerable<StatusMessage> Get(ServerInstallParams installParams,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		ServerInstallContext ctx = new ServerInstallContext(installParams);
		
		yield return new StatusMessage("Installation started");

		foreach (var component in Enum.GetValues<ServerInstallComponent>())
		{
			var installer = installers.FirstOrDefault(i => i.ComponentType == component);
			if (installer == null)
				continue;
			
			cancellationToken.ThrowIfCancellationRequested();

			ctx.ComponentInfos[component] = await installer.GatherInfoAsync(ctx, false, cancellationToken);
			
			if (!ctx.Params.Components.Contains(component))
				continue;

			if (!ctx.ComponentInfos[component].Installable)
			{
				yield return new StatusMessage($"Component <{component}> is not installable, skipping");
				continue;
			}
			
			cancellationToken.ThrowIfCancellationRequested();
			
			yield return new StatusMessage($"Installing component <{component}>");

			await foreach (var message in installer.Install(ctx, cancellationToken))
			{
				yield return message;
			}

			cancellationToken.ThrowIfCancellationRequested();
			
			ctx.ComponentInfos[component] = await installer.GatherInfoAsync(ctx, false, cancellationToken);
			if (!ctx.ComponentInfos[component].Installed)
			{
				yield return new StatusMessage($"Failed to validate component installation <{component}>",
					MessageStatus.Error);
				throw new InstallException();
			}
			
			yield return new StatusMessage($"Component <{component}> installed");
		}

		yield return new StatusMessage("Installation finished successfully", MessageStatus.Success);
	}
	
}