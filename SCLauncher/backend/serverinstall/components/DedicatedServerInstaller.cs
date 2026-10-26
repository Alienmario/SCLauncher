using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DepotDownloader;
using SCLauncher.backend.steam;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.model.config;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

internal class DedicatedServerInstaller(GlobalConfiguration globalConfig)
	: IServerComponentInstaller<ComponentInfo>
{
	private const char VersionDelimiter = '/';

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
		if (ctx.GetTargetVersion(Component) is { } targetVersion)
		{
			cfg.Branch = targetVersion.Split(VersionDelimiter)[0];
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

			LocalInstallInfo? installInfo = null;
			try
			{
				installInfo = DepotDownloaderApi.GetLocalInstallInfo(ctx.InstallPath);
			}
			catch (Exception e)
			{
				e.Log("Failed reading current installation metadata");
			}

			if (installInfo is { State: LocalInstallState.Incomplete })
				return ComponentInfo.ReadyToInstallWithVersionSelect;

			string? localVersion = installInfo is { Branch: { } branch, BuildId: { } buildId }
				? FormatVersion(branch, buildId)
				: null;

			string? upgradeVersion = null;
			if (checkForUpgrades && installInfo is { Branch: { } localBranch, BuildId: { } localBuildId })
			{
				try
				{
					uint latestBuildId = await DepotDownloaderApi.GetAppBuildId(
						ctx.Params.Profile.ServerAppId, localBranch, ct).ConfigureAwait(false);
					if (latestBuildId > 0 && latestBuildId != localBuildId)
					{
						upgradeVersion = FormatVersion(localBranch, latestBuildId);
					}
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
				Version = FormatVersion(b.Name, b.BuildId),
				Title = b.Name,
				Date = b.TimeUpdated,
				Description = string.IsNullOrWhiteSpace(b.Description)
					? $"Build ID: {b.BuildId}"
					: $"{b.Description}\n\nBuild ID: {b.BuildId}"
			})
			.ToList();
	}

	public bool TryCompareVersions(string v1, string v2, [NotNullWhen(true)] out int? result)
	{
		string[] parts1 = v1.Split(VersionDelimiter);
		string[] parts2 = v2.Split(VersionDelimiter);
		if (parts1.Length == 2 && parts2.Length == 2
		    && string.Equals(parts1[0], parts2[0], StringComparison.OrdinalIgnoreCase)
		    && uint.TryParse(parts1[1], out uint b1)
		    && uint.TryParse(parts2[1], out uint b2))
		{
			result = b1.CompareTo(b2);
			return true;
		}

		result = null;
		return false;
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
	
	private static string FormatVersion(string branch, uint buildId) =>
		$"{branch}{VersionDelimiter}{buildId}";
	
}
