using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.backend.install;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

public partial class SourceModInstaller(InstallHelper helper) : IServerComponentInstaller<ComponentInfo>
{
	
	private const string SourceModVersion = "1.12";

	public ServerInstallComponent ComponentType => ServerInstallComponent.SourceMod;
	
	public async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken ct)
	{
		(string url, string filename) dl = await GetDownloadAsync(SourceModVersion, ct);
		string smArchive = Path.Join(ctx.InstallDir, dl.filename);
		
		yield return new StatusMessage($"Downloading...\n URL: {dl.url}\n Target: \"{smArchive}\"");
		
		try
		{
			await helper.DownloadAsync(dl.url, smArchive, ct);
		}
		catch (Exception e)
		{
			helper.SafeDelete(smArchive);
			throw new InstallException("Failed to download SourceMod", e);
		}

		yield return new StatusMessage($"Backing up config files...");
		string? backupArchive = await BackupUserData(ctx, ct);
		if (backupArchive == null)
		{
			yield return new StatusMessage("No files to backup");
		}

		try
		{
			yield return new StatusMessage($"Extracting...");
			try
			{
				await helper.ExtractAsync(smArchive, ctx.GameModDir, true, ct);
			}
			catch (Exception e)
			{
				throw new InstallException("Failed to extract SourceMod", e);
			}

			if (backupArchive != null)
			{
				yield return new StatusMessage($"Restoring config files...");
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
			helper.SafeDelete(smArchive);
			helper.SafeDelete(backupArchive);
		}
	}

	public async Task<string?> BackupUserData(ServerInstallContext ctx, CancellationToken ct)
	{
		string smConfigsPath = Path.Join(ctx.AddonsDir, "sourcemod", "configs");
		string backupFile = Path.Join(ctx.AddonsDir, "sourcemod", "sclauncher_backup.zip");
		helper.SafeDelete(backupFile);

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
			helper.SafeDelete(backupFile);
			throw;
		}
	}

	public async Task RestoreUserData(ServerInstallContext ctx, string backupFile)
	{
		await helper.ExtractAsync(backupFile, Path.Join(ctx.AddonsDir, "sourcemod"), true);
	}
	
	public async Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken ct = default)
	{
		string sourcemod = Path.Join(ctx.AddonsDir, "sourcemod");
		
		if (!Directory.Exists(sourcemod))
		{
			return ComponentInfo.ReadyToInstall;
		}
		
		Version? localVersion = null;
		Version? upgradeVersion = null;
		if (checkForUpgrades)
		{
			try
			{
				localVersion = GetLocalVersion(sourcemod);
				if (localVersion != null)
				{
					Version? latestVersion = await GetLatestVersionAsync(SourceModVersion, ct);
					if (latestVersion != null)
					{
						Trace.WriteLine($"Checking SourceMod version [Local: {localVersion}, Latest: {latestVersion}]");
						if (localVersion.CompareTo(latestVersion) < 0)
						{
							upgradeVersion = latestVersion;
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
			Path = sourcemod,
			Upgradable = upgradeVersion != null,
			UpgradeVersion = upgradeVersion?.ToString(),
			Version = localVersion?.ToString()
		};
	}
	
	private async Task<(string url, string filename)> GetDownloadAsync(string baseVersion,
		CancellationToken ct = default)
	{
		string platform = Environment.OSVersion.Platform switch
		{
			PlatformID.Win32NT => "windows",
			PlatformID.Unix => "linux",
			_ => throw new PlatformNotSupportedException()
		};
		string baseUrl = $"https://sm.alliedmods.net/smdrop/{baseVersion}/";
		string filename = await helper.HttpClient.GetStringAsync($"{baseUrl}sourcemod-latest-{platform}", ct);
		return (baseUrl + filename, filename);
	}

	[GeneratedRegex(@"^(\d+)\.(\d+)\..*\.(\d+)$")]
	private static partial Regex ProductVersionRegex();
	
	private Version? GetLocalVersion(string sourcemod)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			string sourcemodDll = Path.Join(sourcemod, "bin", "sourcemod.logic.dll");
			string? productVersion = helper.GetProductVersion(sourcemodDll);
			if (productVersion != null)
			{
				Match match = ProductVersionRegex().Match(productVersion);
				return ExtractVersion(match);
			}
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			// wat do
		}

		return null;
	}

	[GeneratedRegex(@"^sourcemod-(\d+)\.(\d+)\..*-git(\d+)")]
	private static partial Regex DlFilenameVersionRegex();
	
	private async Task<Version?> GetLatestVersionAsync(string baseVersion, CancellationToken ct = default)
	{
		var dl = await GetDownloadAsync(baseVersion, ct);
		Match match = DlFilenameVersionRegex().Match(dl.filename);
		return ExtractVersion(match);
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