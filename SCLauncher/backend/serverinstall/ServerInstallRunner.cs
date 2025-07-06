using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall;

public class ServerInstallRunner(IEnumerable<IServerComponentInstaller<ComponentInfo>> installers)
{
	internal async IAsyncEnumerable<StatusMessage> Installer(ServerInstallParams installParams,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		ServerInstallContext ctx = new ServerInstallContext(installParams);
		
		yield return new StatusMessage("Installation started");

		foreach (var component in Enum.GetValues<ServerInstallComponent>())
		{
			var installer = installers.FirstOrDefault(i => i.ComponentType == component);
			if (installer == null)
				continue;
			
			ct.ThrowIfCancellationRequested();

			ctx.ComponentInfos[component] = await installer.GatherInfoAsync(ctx, false, ct);
			
			if (!ctx.Params.Components.Contains(component))
				continue;

			if (!ctx.ComponentInfos[component].Installable)
			{
				yield return new StatusMessage($"Component <{component}> is not installable, skipping");
				continue;
			}
			
			ct.ThrowIfCancellationRequested();
			
			yield return new StatusMessage($"Installing component <{component}>");

			await foreach (var message in installer.Install(ctx, ct))
			{
				yield return message;
			}

			ct.ThrowIfCancellationRequested();
			
			ctx.ComponentInfos[component] = await installer.GatherInfoAsync(ctx, false, ct);
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

	internal async IAsyncEnumerable<StatusMessage> Uninstaller(ServerUninstallParams uninstallParams,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		ServerUninstallContext ctx = new ServerUninstallContext(uninstallParams);
		
		yield return new StatusMessage("Uninstall started");

		foreach (var component in Enum.GetValues<ServerInstallComponent>())
		{
			var installer = installers.FirstOrDefault(i => i.ComponentType == component);
			if (installer == null)
				continue;

			ct.ThrowIfCancellationRequested();

			IAsyncEnumerable<StatusMessage> componentUninstaller;
			try
			{
				componentUninstaller = installer.Uninstall(ctx, ct);
			}
			catch (NotImplementedException)
			{
				continue;
			}
			
			yield return new StatusMessage($"Uninstalling component <{component}>");
			await foreach (var message in componentUninstaller.WithCancellation(ct))
			{
				yield return message;
			}
			yield return new StatusMessage($"Component <{component}> uninstalled");
		}

		if (!Directory.Exists(ctx.Params.Path))
		{
			throw new InstallException("Server directory is not valid");
		}
		
		yield return new StatusMessage("Deleting server directory");
		try
		{
			Directory.Delete(ctx.Params.Path, true);
		}
		catch (Exception e)
		{
			throw new InstallException("Unable to delete server directory", e);
		}

		yield return new StatusMessage("Uninstall finished successfully", MessageStatus.Success);
	}
}