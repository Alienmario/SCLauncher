using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.backend.install;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall;

public class ServerInstallRunner(IEnumerable<IServerComponentInstaller<ComponentInfo>> installers)
{
	internal async IAsyncEnumerable<object> Installer(ServerInstallParams installParams,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		ServerInstallContext ctx = new ServerInstallContext(installParams);

		yield return new ProgressUpdate { Text = "Installation started", NumSteps = ctx.Params.Components.Count + 1 };
		yield return new StatusMessage($"Installer started{Environment.NewLine}");
		int step = 1;
		
		try
		{
			foreach (var component in ctx.Params.Components.OrderBy(component => component.InstallOrder))
			{
				async Task GatherInfoRecursive(ServerInstallComponent c)
				{
					foreach (var dependency in c.Dependencies)
					{
						await GatherInfoRecursive(dependency);
					}
					if (!ctx.ComponentInfos.ContainsKey(c))
					{
						ct.ThrowIfCancellationRequested();
						ctx.ComponentInfos[c] = await installers.Single(i => i.Component == c)
							.GatherInfoAsync(ctx, false, ct);
					}
				}

				await GatherInfoRecursive(component);
				ct.ThrowIfCancellationRequested();

				yield return new ProgressUpdate { Text = $"Installing {component}", Step = step++ };
				yield return new StatusMessage($"Installing {component}");
				
				if (!ctx.ComponentInfos[component].Installable)
				{
					yield return new StatusMessage($"Component \"{component}\" is not installable, skipping");
					continue;
				}
				
				var installer = installers.Single(i => i.Component == component);
				await foreach (var message in installer.Install(ctx, ct))
				{
					yield return message;
				}

				ct.ThrowIfCancellationRequested();

				ctx.ComponentInfos[component] = await installer.GatherInfoAsync(ctx, false, ct);
				if (!ctx.ComponentInfos[component].Installed)
				{
					throw new InstallException($"Failed to validate component installation: {component}");
				}

				yield return new StatusMessage($"{component} installed{Environment.NewLine}");
			}
		}
		finally
		{
			// Save install path in the profile - even when install fails
			try
			{
				ctx.Params.Profile.ServerPath = ctx.InstallPath;
			}
			catch (UnsetInstallPathException) {}
		}

		yield return new StatusMessage("Installer finished successfully", MessageStatus.Success);
		yield return new ProgressUpdate { Text = "All done!", Step = step };
	}

	internal async IAsyncEnumerable<object> Uninstaller(ServerUninstallParams uninstallParams,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		ServerUninstallContext ctx = new ServerUninstallContext(uninstallParams);
		
		yield return new ProgressUpdate { Text = "Uninstallation started", NumSteps = installers.Count() + 2 };
		yield return new StatusMessage($"Uninstaller started{Environment.NewLine}");
		int step = 1;

		foreach (var installer in installers.OrderByDescending(installer => installer.Component.InstallOrder))
		{
			ct.ThrowIfCancellationRequested();

			var component = installer.Component;
			IAsyncEnumerable<StatusMessage> componentUninstaller;
			try
			{
				componentUninstaller = installer.Uninstall(ctx, ct);
			}
			catch (NotImplementedException)
			{
				step++;
				continue;
			}
			
			yield return new ProgressUpdate { Text = $"Uninstalling {component}", Step = step++ };
			yield return new StatusMessage($"Uninstalling {component}");
			
			await foreach (var message in componentUninstaller.WithCancellation(ct))
			{
				yield return message;
			}
			yield return new StatusMessage($"{component} uninstalled{Environment.NewLine}");
		}

		if (!Directory.Exists(ctx.Params.Path))
		{
			throw new InstallException("Server directory does not exist");
		}
		
		yield return new StatusMessage("Deleting server directory");
		yield return new ProgressUpdate { Text = "Deleting server directory", Step = step++ };
		
		try
		{
			Directory.Delete(ctx.Params.Path, true);
		}
		catch (Exception e)
		{
			e.Log();
			throw new InstallException("Unable to delete the server directory", e);
		}

		yield return new StatusMessage("Uninstaller finished successfully", MessageStatus.Success);
		yield return new ProgressUpdate { Text = "All done!", Step = step };
	}
}