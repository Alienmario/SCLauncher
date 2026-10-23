using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace SCLauncher.backend.serverinstall.components;

internal class DedicatedServerInstaller(GlobalConfiguration globalConfig, InstallHelper installHelper)
	: IServerComponentInstaller<ComponentInfo>
{
	public class DedicatedServerComponentInfo : ComponentInfo
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
		if (!SteamUtils.IsValidSteamInstallPath(globalConfig.SteamPath))
		{
			throw new InstallException("Steam not found! Install it or specify Steam path in settings, then retry.");
		}

		SteamUtils.InstallApp(ctx.Params.Profile.ServerAppId);
		yield return new StatusMessage($"Waiting for Steam to finish downloading \"{ctx.Params.Profile.ServerInstallFolder}\"");
		
		while (true)
		{
			await Task.Delay(1000, ct);
			ComponentInfo info = await GatherInfoAsync(ctx, false, ct);
			
			if (info is DedicatedServerComponentInfo { Validate: true })
			{
				yield return new StatusMessage("Requesting Steam validation");
				SteamUtils.ValidateApp(ctx.Params.Profile.ServerAppId);
			}
			else if (info.Installed)
			{
				break;
			}
		}
	}

	private async IAsyncEnumerable<StatusMessage> InstallViaDepotDownloader(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		var cfg = new AppDownloadConfig
		{
			InstallDirectory = ctx.InstallPath,
			VerifyAll = true,
			AppId = ctx.Params.Profile.ServerAppId
		};
		if (ctx.GetTargetVersion(Component) is { } branch)
		{
			cfg.Branch = branch;
		}

		await using var enumerator = DepotDownloaderApi.AppDownload(cfg, null, ct)
			.ConfigureAwait(false).GetAsyncEnumerator();

		while (true)
		{
			try
			{
				if (!await enumerator.MoveNextAsync())
					break;
			}
			catch (DepotDownloaderApiException e)
			{
				string messages = e.GetAllMessages();
				if (messages != "")
					throw new InstallException($"Dedicated server download failed ({messages})");
				throw new InstallException("Dedicated server download failed");
			}

			yield return new StatusMessage(enumerator.Current.msg);
		}
	}
	
	public async Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken ct = default)
	{
		if (ctx.Params.Method == ServerInstallMethod.Steam)
		{
			string? steamPath = globalConfig.SteamPath;
			if (steamPath == null) // this is explicitly handled during install
				return ComponentInfo.ReadyToInstall;

			SteamAppManifest? manifest = await SteamUtils.FindAppManifestAsync(steamPath, ctx.Params.Profile.ServerAppId, ct);
			if (manifest == null)
				return ComponentInfo.ReadyToInstall;

			string? absInstallPath = manifest.GetAbsInstallPath();
			if (absInstallPath == null)
				return ComponentInfo.ReadyToInstall;
			
			bool fullyInstalled =
				manifest.StateFlags.HasFlag(SteamAppState.StateFullyInstalled)
				&& !manifest.StateFlags.HasFlag(SteamAppState.StateUpdateStarted);
			
			bool dirExists = Directory.Exists(absInstallPath);
			
			return new DedicatedServerComponentInfo
			{
				Installable = true,
				Installed = fullyInstalled && dirExists,
				Path = absInstallPath,
				Validate = !dirExists
			};
		}
		else // ServerInstallMethod.Standalone
		{
			if (!File.Exists(Path.Join(ctx.GameModPath, "gameinfo.txt")))
				return ComponentInfo.ReadyToInstallWithVersionSelect;
			
			string? localVersion = null;
			try
			{
				localVersion = await GetLocalPatchVersionAsync(ctx, ct);
			}
			catch (Exception e)
			{
				e.Log("Failed reading local server patch version");
			}

			string? upgradeVersion = null;
			if (checkForUpgrades && localVersion != null)
			{
				try
				{
					// TODO: Transition to steam build id comparison
					// GMOD - System.FormatException: The input string '2025.03.26' was not in a correct format.
					// NMRIH - System.FormatException: The input string '1.1.5.0' was not in a correct format.
					var uintVersion = Convert.ToUInt32(localVersion);
					upgradeVersion = await SteamUpToDateCheckAsync(ctx.Params.Profile.GameAppId, uintVersion, ct);
				}
				catch (Exception e)
				{
					e.Log("Failed server up-to-date check");
				}
			}

			return new ComponentInfo
			{
				Path = ctx.InstallPath,
				Version = localVersion,
				UpgradeVersion = upgradeVersion,
				Upgradable = upgradeVersion != null,
				VersionSelectable = true
			};
		}
	}

	public async Task<IReadOnlyList<ComponentVersion>> GatherVersionsAsync(
		ServerInstallContext ctx, CancellationToken ct = default)
	{
		if (ctx.Params.Method != ServerInstallMethod.Standalone)
			return [];

		var branches = await DepotDownloaderApi.GetAppBranches(ctx.Params.Profile.ServerAppId, ct)
			.ConfigureAwait(false);

		return branches
			.Where(b => !b.PasswordRequired)
			.OrderByDescending(b => string.Equals(b.Name, "public", StringComparison.OrdinalIgnoreCase))
			.Select(b => new ComponentVersion
			{
				Version = b.Name,
				Title = b.Name,
				Date = b.TimeUpdated,
				Description = string.IsNullOrWhiteSpace(b.Description)
					? $"Build ID: {b.BuildId}"
					: $"{b.Description}\n\nBuild ID: {b.BuildId}"
			})
			.ToList();
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
	
	private static async Task<string?> GetLocalPatchVersionAsync(ServerInstallContext ctx, CancellationToken ct = default)
	{
		string steaminfPath = Path.Join(ctx.GameModPath, "steam.inf");
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
