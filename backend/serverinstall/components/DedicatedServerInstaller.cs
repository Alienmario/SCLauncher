using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DepotDownloader;
using SCLauncher.backend.service;
using SCLauncher.backend.steam;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;
using SteamKit2.Authentication;

namespace SCLauncher.backend.serverinstall.components;

public class DedicatedServerInstaller(BackendService backend) : IServerComponentInstaller<ComponentInfo>
{
	
	public ServerInstallComponent ComponentType => ServerInstallComponent.Server;
	
	public IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx, CancellationToken cancellationToken = default)
	{
		if (ctx.Params.Method == ServerInstallMethod.Steam)
		{
			return InstallViaSteamClient(ctx, cancellationToken);
		}
		else
		{
			return InstallViaDepotDownloader(ctx, cancellationToken);
		}
	}

	public async IAsyncEnumerable<StatusMessage> InstallViaSteamClient(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (!Directory.Exists(backend.GetSteamDir()))
		{
			yield return new StatusMessage(
				"Steam not found! Install it or adjust path in settings, then retry.", MessageStatus.Error);
			yield break;
		}

		SteamUtils.InstallApp(ctx.Params.AppInfo.ServerAppId);
		yield return new StatusMessage("Waiting for Steam to finish downloading");
		
		while (true)
		{
			await Task.Delay(1000, cancellationToken);
			ComponentInfo info = await GatherInfoAsync(ctx, false, cancellationToken);
			if (info.Installed)
			{
				break;
			}
		}
	}

	private static async IAsyncEnumerable<StatusMessage> InstallViaDepotDownloader(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var cfg = new AppDownloadConfig
		{
			InstallDirectory = ctx.InstallDir,
			VerifyAll = true,
			AppId = (uint)ctx.Params.AppInfo.ServerAppId
		};

		int? exitCode = null;
		await foreach ((string message, bool error) in SubProcess.AppDownload(cfg,
			               i => exitCode = i,
			               new UserConsoleAuthenticator(),
			               cancellationToken))
		{
			yield return new StatusMessage(message);
		}

		if (exitCode != SubProcess.Success)
		{
			yield return new StatusMessage("Download failed", MessageStatus.Error);
		}
	}
	
	public async Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken cancellationToken = default)
	{
		if (ctx.Params.Method == ServerInstallMethod.Steam)
		{
			// this is explicitly checked during install
			string? steamDir = backend.GetSteamDir();
			if (steamDir == null)
				return ComponentInfo.ReadyToInstall;

			SteamAppManifest? manifest = await SteamUtils.FindAppManifestAsync(steamDir, ctx.Params.AppInfo.ServerAppId, cancellationToken);
			if (manifest == null)
				return ComponentInfo.ReadyToInstall;
			
			// in-between states, passing ReadyToInstall is fine, nothing weird will happen
			if (!manifest.StateFlags.HasFlag(SteamAppState.StateFullyInstalled))
				return ComponentInfo.ReadyToInstall;

			string? absInstallPath = manifest.GetAbsInstallPath();
			if (absInstallPath == null)
				return ComponentInfo.ReadyToInstall;

			return new ComponentInfo
			{
				Path = absInstallPath
			};
		}
		else
		{
			if (!Directory.Exists(ctx.InstallDir))
				return ComponentInfo.ReadyToInstall;

			string? version = null;
			try {
				foreach (string dir in Directory.EnumerateDirectories(ctx.InstallDir))
				{
					string steaminfPath = Path.Combine(dir, "steam.inf");
					if (File.Exists(steaminfPath))
					{
						foreach (string line in await File.ReadAllLinesAsync(steaminfPath, cancellationToken))
						{
							if (line.StartsWith("PatchVersion="))
							{
								version = line["PatchVersion=".Length..];
								break;
							}
						}
					}
				}
			}
			catch (IOException) {}

			// if we didn't find the version, installation may be incomplete
			if (version == null)
				return ComponentInfo.ReadyToInstall;

			return new ComponentInfo
			{
				Path = ctx.InstallDir,
				Version = version
			};
		}
	}

}