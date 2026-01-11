using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DepotDownloader;
using SCLauncher.backend.install;
using SCLauncher.backend.steam;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.model.config;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;
using SteamKit2.Authentication;

namespace SCLauncher.backend.serverinstall.components;

public class DedicatedServerInstaller(GlobalConfiguration globalConfig, InstallHelper installHelper)
	: IServerComponentInstaller<ComponentInfo>
{
	public class ServerComponentInfo : ComponentInfo
	{
		public bool Validate { get; init; }
	}
	
	public ServerInstallComponent Component => ServerInstallComponent.Server;
	
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
		if (!SteamUtils.IsValidSteamInstallDir(globalConfig.SteamPath))
		{
			throw new InstallException("Steam not found! Install it or specify Steam path in settings, then retry.");
		}

		SteamUtils.InstallApp(ctx.Params.Profile.ServerAppId);
		yield return new StatusMessage($"Waiting for Steam to finish downloading \"{ctx.Params.Profile.ServerInstallFolder}\"");
		
		while (true)
		{
			await Task.Delay(1000, ct);
			ComponentInfo info = await GatherInfoAsync(ctx, false, ct);
			
			if (info is ServerComponentInfo { Validate: true })
			{
				yield return new StatusMessage("Requesting Steam validation");
				SteamUtils.ValidateApp(ctx.Params.Profile.ServerAppId);
			}
			else if (info.Installed)
			{
				ctx.Params.Profile.ServerPath = info.Path;
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
			AppId = ctx.Params.Profile.ServerAppId
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

		ctx.Params.Profile.ServerPath = ctx.InstallDir;
	}
	
	public async Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken ct = default)
	{
		if (ctx.Params.Method == ServerInstallMethod.Steam)
		{
			string? steamDir = globalConfig.SteamPath;
			if (steamDir == null) // this is explicitly handled during install
				return ComponentInfo.ReadyToInstall;

			SteamAppManifest? manifest = await SteamUtils.FindAppManifestAsync(steamDir, ctx.Params.Profile.ServerAppId, ct);
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
			}
			catch (Exception e)
			{
				e.Log();
			}

			if (version == null)
			{
				Trace.WriteLine("Failed to find server patch version, considering installation as incomplete");
				return ComponentInfo.ReadyToInstall;
			}

			string? upgradeVersion = null;
			if (checkForUpgrades)
			{
				try
				{
					var uintVersion = Convert.ToUInt32(version);
					upgradeVersion = await SteamUpToDateCheckAsync(ctx.Params.Profile.ServerAppId, uintVersion, ct);
				}
				catch (Exception e)
				{
					e.Log();
				}
			}

			return new ComponentInfo
			{
				Path = ctx.InstallDir,
				Version = version,
				UpgradeVersion = upgradeVersion,
				Upgradable = upgradeVersion != null
			};
		}
	}

	public async IAsyncEnumerable<StatusMessage> Uninstall(ServerUninstallContext ctx,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		bool steamRequestDispatched = false;
		
		do
		{
			string? steamDir = globalConfig.SteamPath;
			if (steamDir == null)
				yield break;
			
			var manifest = await SteamUtils.FindAppManifestAsync(steamDir, ctx.Params.Profile.ServerAppId, ct);
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
				SteamUtils.UninstallApp(ctx.Params.Profile.ServerAppId);
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

	public async Task<string?> SteamUpToDateCheckAsync(uint appId, uint version, CancellationToken cancellationToken = default)
	{
		var url = $"https://api.steampowered.com/ISteamApps/UpToDateCheck/v1/?appid={appId}&version={version}";
		var json = await installHelper.HttpClient.GetStringAsync(url, cancellationToken);

		// Parse JSON for required_version
		var doc = JsonDocument.Parse(json);
		if (doc.RootElement.TryGetProperty("response", out var respElem) &&
		    respElem.TryGetProperty("required_version", out var reqVerElem))
		{
			return reqVerElem.ToString();
		}
		return null;
	}
	
}