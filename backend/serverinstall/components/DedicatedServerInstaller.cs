using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DepotDownloader;
using SCLauncher.backend.steam;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;
using SteamKit2.Authentication;

namespace SCLauncher.backend.serverinstall.components;

public class DedicatedServerInstaller(GlobalConfiguration config) : IServerComponentInstaller<ComponentInfo>
{
	public class ServerComponentInfo : ComponentInfo
	{
		public bool Validate { get; init; }
	}
	
	public ServerInstallComponent ComponentType => ServerInstallComponent.Server;
	
	public IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx, CancellationToken ct = default)
	{
		if (ctx.Params.Method == ServerInstallMethod.Steam)
		{
			return InstallViaSteamClient(ctx, ct);
		}
		else
		{
			return InstallViaDepotDownloader(ctx, ct);
		}
	}

	public async IAsyncEnumerable<StatusMessage> InstallViaSteamClient(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		if (!SteamUtils.IsValidSteamInstallDir(config.SteamPath))
		{
			throw new InstallException("Steam not found! Install it or specify Steam path in settings, then retry.");
		}

		SteamUtils.InstallApp(ctx.Params.AppInfo.ServerAppId);
		yield return new StatusMessage($"Waiting for Steam to finish downloading \"{ctx.Params.AppInfo.ServerInstallFolder}\"");
		
		while (true)
		{
			await Task.Delay(1000, ct);
			ComponentInfo info = await GatherInfoAsync(ctx, false, ct);
			
			if (info is ServerComponentInfo { Validate: true })
			{
				yield return new StatusMessage("Requesting Steam validation");
				SteamUtils.ValidateApp(ctx.Params.AppInfo.ServerAppId);
			}
			else if (info.Installed)
			{
				config.ServerPath = info.Path;
				break;
			}
		}
	}

	private async IAsyncEnumerable<StatusMessage> InstallViaDepotDownloader(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken ct = default)
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
			               ct))
		{
			yield return new StatusMessage(message);
		}

		if (exitCode != SubProcess.Success)
		{
			throw new InstallException("Dedicated server download failed");
		}

		config.ServerPath = ctx.InstallDir;
	}
	
	public async Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken ct = default)
	{
		if (ctx.Params.Method == ServerInstallMethod.Steam)
		{
			string? steamDir = config.SteamPath;
			if (steamDir == null) // this is explicitly handled during install
				return ComponentInfo.ReadyToInstall;

			SteamAppManifest? manifest = await SteamUtils.FindAppManifestAsync(steamDir, ctx.Params.AppInfo.ServerAppId, ct);
			if (manifest == null)
				return ComponentInfo.ReadyToInstall;

			string? absInstallPath = manifest.GetAbsInstallPath();
			if (absInstallPath == null)
				return ComponentInfo.ReadyToInstall;
			
			bool fullyInstalled =
				manifest.StateFlags.HasFlag(SteamAppState.StateFullyInstalled)
				&& !manifest.StateFlags.HasFlag(SteamAppState.StateUpdateStarted);
			
			bool dirExists = Directory.Exists(absInstallPath);
			
			return new ServerComponentInfo
			{
				Installable = true,
				Installed = fullyInstalled && dirExists,
				Path = absInstallPath,
				Validate = !dirExists
			};
		}
		else // ServerInstallMethod.External
		{
			if (!Directory.Exists(ctx.InstallDir))
				return ComponentInfo.ReadyToInstall;

			string? version = null;
			try
			{
				version = await GetLocalPatchVersionAsync(ctx.InstallDir, ct);
				// int? latestBuildNumber = await GetLatestBuildNumberAsync(ctx.Params.AppInfo.ServerAppId, ct);
			}
			catch (Exception e)
			{
				e.Log();
			}

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

	public async IAsyncEnumerable<StatusMessage> Uninstall(ServerUninstallContext ctx,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		bool steamRequestDispatched = false;
		
		do
		{
			string? steamDir = config.SteamPath;
			if (steamDir == null)
				yield break;
			
			var manifest = await SteamUtils.FindAppManifestAsync(steamDir, ctx.Params.AppInfo.ServerAppId, ct);
			if (manifest == null)
				yield break;

			string? absInstallPath = manifest.GetAbsInstallPath();
			if (absInstallPath == null)
				yield break;

			try
			{
				if (!Path.GetFullPath(absInstallPath).Equals(Path.GetFullPath(ctx.Params.Path), StringComparison.OrdinalIgnoreCase))
					yield break;
			}
			catch (Exception e)
			{
				e.Log();
				yield break;
			}

			// we've determined that this is a steam installation, issue the uninstall command and wait

			if (!steamRequestDispatched)
			{
				SteamUtils.UninstallApp(ctx.Params.AppInfo.ServerAppId);
				yield return new StatusMessage("Waiting for Steam uninstaller to finish");
				steamRequestDispatched = true;
			}

			await Task.Delay(1000, ct);
		}
		while (true);
	}
	
	private static async Task<string?> GetLocalPatchVersionAsync(string installDir, CancellationToken ct = default)
	{
		foreach (string dir in Directory.EnumerateDirectories(installDir))
		{
			string steaminfPath = Path.Combine(dir, "steam.inf");
			if (File.Exists(steaminfPath))
			{
				foreach (string line in await File.ReadAllLinesAsync(steaminfPath, ct))
				{
					if (line.StartsWith("PatchVersion="))
					{
						return line["PatchVersion=".Length..];
					}
				}
			}
		}

		return null;
	}

	/*
	private static async Task<int?> GetLatestBuildNumberAsync(int appId, CancellationToken ct = default)
	{
		int buildNumber = await SubProcess.GetAppBuildNumber(new GetAppBuildNumberConfig { AppId = (uint)appId },
			cancellationToken: ct);
		if (buildNumber <= 0)
			return null;
		return buildNumber;
	}
	*/
}