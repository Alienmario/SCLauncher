using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DepotDownloader;
using SCLauncher.backend.service;
using SCLauncher.backend.util.steam;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;
using SteamKit2.Authentication;

namespace SCLauncher.backend.serverinstall.components;

public class ServerInstaller(BackendService backend) : IServerComponentInstaller<ComponentInfo>
{
	
	public ServerInstallComponent Type => ServerInstallComponent.Server;
	
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

		SteamUtils.InstallApp(ctx.Params.AppId);
		yield return new StatusMessage("Waiting for Steam to finish downloading");
		
		while (true)
		{
			await Task.Delay(1000, cancellationToken);
			ComponentInfo? info = await GatherInfo(ctx, cancellationToken);
			if (info != null)
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
			AppId = (uint)ctx.Params.AppId
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
	
	public async Task<ComponentInfo?> GatherInfo(ServerInstallContext ctx, CancellationToken cancellationToken = default)
	{
		if (ctx.Params.Method == ServerInstallMethod.Steam)
		{
			string? steamDir = backend.GetSteamDir();
			if (steamDir == null)
				return null;

			SteamAppManifest? manifest = await SteamUtils.FindAppManifestAsync(steamDir, ctx.Params.AppId, cancellationToken);
			if (manifest == null)
				return null;
			
			if (!manifest.StateFlags.HasFlag(SteamAppState.StateFullyInstalled))
				return null;

			string? absInstallPath = manifest.GetAbsInstallPath();
			if (absInstallPath == null)
				return null;

			return new ComponentInfo
			{
				Path = absInstallPath
			};
		}
		else
		{
			if (!Directory.Exists(ctx.InstallDir))
				return null;

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
				return null;

			return new ComponentInfo
			{
				Path = ctx.InstallDir,
				Version = version
			};
		}
	}

}