using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using SCLauncher.backend.install;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

internal partial class SourceModInstaller : BaseGithubServerComponentInstaller
{
	[GeneratedRegex(@"^sourcemod.*-windows.*\.(?:tar\.gz|tgz|zip)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex AssetRegexWindows { get; }
	
	[GeneratedRegex(@"^sourcemod.*-linux.*\.(?:tar\.gz|tgz|zip)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex AssetRegexLinux { get; }

	[SetsRequiredMembers]
	public SourceModInstaller(InstallHelper helper) : base(helper)
	{
		Component = ServerInstallComponent.SourceMod;
		GithubOwner = "alliedmodders";
		GithubRepo = "sourcemod";
		
		if (OperatingSystem.IsWindows())
			GithubAssetRegex = AssetRegexWindows;
		else if (OperatingSystem.IsLinux())
			GithubAssetRegex = AssetRegexLinux;
		else throw new PlatformNotSupportedException();
	}

	protected override async IAsyncEnumerable<StatusMessage> ExtractArchiveAsync(
		ServerInstallContext ctx, string archivePath,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		yield return new StatusMessage("Backing up config files...");
		string? backupArchive = await BackupUserData(ctx, ct).ConfigureAwait(false);
		if (backupArchive == null)
		{
			yield return new StatusMessage("No files to backup");
		}

		try
		{
			await foreach (var msg in base.ExtractArchiveAsync(ctx, archivePath, ct).ConfigureAwait(false))
			{
				yield return msg;
			}

			if (backupArchive != null)
			{
				yield return new StatusMessage("Restoring config files...");
				try
				{
					await RestoreUserData(ctx, backupArchive).ConfigureAwait(false);
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
			Helper.SafeDelete(backupArchive);
		}
	}

	private async Task<string?> BackupUserData(ServerInstallContext ctx, CancellationToken ct)
	{
		string smConfigsPath = Path.Join(ctx.AddonsPath, "sourcemod", "configs");
		string backupFile = Path.Join(ctx.AddonsPath, "sourcemod", "sclauncher_backup.zip");
		Helper.SafeDelete(backupFile);

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
			Helper.SafeDelete(backupFile);
			throw;
		}
	}

	private async Task RestoreUserData(ServerInstallContext ctx, string backupFile)
	{
		await Helper.ExtractAsync(backupFile, Path.Join(ctx.AddonsPath, "sourcemod"), true);
	}

	protected override string GetComponentPath(ServerInstallContext ctx) =>
		Path.Join(ctx.AddonsPath, "sourcemod");

	protected override string? GetLocalVersion(string path)
	{
		if (OperatingSystem.IsWindows())
		{
			string dll = Path.Join(path, "bin", "sourcemod.logic.dll");
			string? productVersion = Helper.GetFileProductVersion(dll);
			if (productVersion != null)
			{
				return ExtractAlliedModdersVersion(productVersion);
			}
		}
		else if (OperatingSystem.IsLinux())
		{
			// wat do
		}
		else throw new PlatformNotSupportedException();

		return null;
	}

	public override string? GetVersionString(Release release) => ExtractAlliedModdersVersion(release.TagName);
}
