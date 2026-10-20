using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using SCLauncher.backend.install;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

internal partial class SourceModInstaller : GithubInstaller<ServerInstallContext>, IServerComponentInstaller<ComponentInfo>
{
	[GeneratedRegex(@"^sourcemod.*-windows.*\.(?:tar\.gz|tgz|zip)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex AssetRegexWindows { get; }
	
	[GeneratedRegex(@"^sourcemod.*-linux.*\.(?:tar\.gz|tgz|zip)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex AssetRegexLinux { get; }
	
	[GeneratedRegex(@"^(\d+)\.(\d+)\..*[\.\+](\d+)$")]
	private static partial Regex VersionRegex { get; }

	public ServerInstallComponent Component => ServerInstallComponent.SourceMod;

	private readonly InstallHelper _helper;

	[SetsRequiredMembers]
	public SourceModInstaller(InstallHelper helper) : base(helper)
	{
		_helper = helper;
		
		GithubOwner = "alliedmodders";
		GithubRepo = "sourcemod";
		
		if (OperatingSystem.IsWindows())
			GithubAssetRegex = AssetRegexWindows;
		else if (OperatingSystem.IsLinux())
			GithubAssetRegex = AssetRegexLinux;
		else throw new PlatformNotSupportedException();
	}

	public async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken ct)
	{
		(Release release, ReleaseAsset asset)? entry;
		try
		{
			entry = await GetGithubDownloadableAsync(false, ctx, ct).ConfigureAwait(false);
		}
		catch (Exception e)
		{
			throw new InstallException($"Unable to read {Component} releases from GitHub", e);
		}

		if (entry == null)
		{
			throw new InstallException("No downloadable archive found in GitHub releases");
		}

		string dlFilename = entry.Value.asset.Name;
		string dlUrl = entry.Value.asset.BrowserDownloadUrl;
		string targetArchive = Path.Join(ctx.InstallPath, dlFilename);
		
		yield return new StatusMessage($"Downloading...\n URL: {dlUrl}\n Target: \"{targetArchive}\"");
		
		try
		{
			await _helper.DownloadAsync(dlUrl, targetArchive, ct);
		}
		catch (Exception e)
		{
			_helper.SafeDelete(targetArchive);
			throw new InstallException($"Failed to download {Component}", e);
		}
		
		ct.ThrowIfCancellationRequested();

		yield return new StatusMessage("Backing up config files...");
		string? backupArchive = await BackupUserData(ctx, ct);
		if (backupArchive == null)
		{
			yield return new StatusMessage("No files to backup");
		}

		try
		{
			yield return new StatusMessage("Extracting...");
			try
			{
				await _helper.ExtractAsync(targetArchive, ctx.GameModPath, true, ct);
			}
			catch (Exception e)
			{
				throw new InstallException($"Failed to extract {dlFilename}", e);
			}

			if (backupArchive != null)
			{
				yield return new StatusMessage("Restoring config files...");
				try
				{
					await RestoreUserData(ctx, backupArchive);
				}
				catch (Exception e)
				{
					backupArchive = null;
					throw new InstallException("Failed to restore SourceMod config from backup", e);
				}
			}
		}
		finally
		{
			_helper.SafeDelete(targetArchive);
			_helper.SafeDelete(backupArchive);
		}
	}

	private async Task<string?> BackupUserData(ServerInstallContext ctx, CancellationToken ct)
	{
		string smConfigsPath = Path.Join(ctx.AddonsPath, "sourcemod", "configs");
		string backupFile = Path.Join(ctx.AddonsPath, "sourcemod", "sclauncher_backup.zip");
		_helper.SafeDelete(backupFile);

		try
		{
			await Task.Run(() => ZipFile.CreateFromDirectory(
				smConfigsPath, backupFile, CompressionLevel.NoCompression, true), ct);
			return backupFile;
		}
		catch (DirectoryNotFoundException)
		{
			return null;
		}
		catch (Exception)
		{
			_helper.SafeDelete(backupFile);
			throw;
		}
	}

	private async Task RestoreUserData(ServerInstallContext ctx, string backupFile)
	{
		await _helper.ExtractAsync(backupFile, Path.Join(ctx.AddonsPath, "sourcemod"), true);
	}
	
	public async Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken ct = default)
	{
		string? path = null;
		try
		{
			path = Path.Join(ctx.AddonsPath, "sourcemod");
		}
		catch (UnsetInstallPathException) {}
		
		if (!Directory.Exists(path))
		{
			return ComponentInfo.ReadyToInstall;
		}
		
		Version? localVersion = null;
		Version? upgradeVersion = null;
		if (checkForUpgrades)
		{
			try
			{
				localVersion = GetLocalVersion(path);
				if (localVersion != null)
				{
					(Release release, ReleaseAsset asset)? entry = await GetGithubDownloadableAsync(false, ctx, ct)
						.ConfigureAwait(false);
					
					if (entry != null)
					{
						Version? latestVersion = ExtractVersion(VersionRegex.Match(entry.Value.release.TagName));
						if (latestVersion != null)
						{
							Trace.WriteLine($"Checking {Component} version [Local: {localVersion}, Latest: {latestVersion}]");
							if (localVersion.CompareTo(latestVersion) < 0)
							{
								upgradeVersion = latestVersion;
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				e.Log();
			}
		}
		
		return new ComponentInfo
		{
			Path = path,
			Upgradable = upgradeVersion != null,
			UpgradeVersion = upgradeVersion?.ToString(),
			Version = localVersion?.ToString()
		};
	}
	
	private Version? GetLocalVersion(string path)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			string dll = Path.Join(path, "bin", "sourcemod.logic.dll");
			string? productVersion = _helper.GetFileProductVersion(dll);
			if (productVersion != null)
			{
				Match match = VersionRegex.Match(productVersion);
				return ExtractVersion(match);
			}
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			// wat do
		}

		return null;
	}
	
	private static Version? ExtractVersion(Match match)
	{
		if (match.Success)
		{
			return new Version(
				int.TryParse(match.Groups[1].Value, out int major) ? major : 0,
				int.TryParse(match.Groups[2].Value, out int minor) ? minor : 0,
				int.TryParse(match.Groups[3].Value, out int build) ? build : 0
			);
		}

		return null;
	}
}