using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

internal abstract partial class BaseGithubServerComponentInstaller(InstallHelper helper)
	: GithubInstaller<ServerInstallContext>(helper), IServerComponentInstaller<ComponentInfo>
{
	[GeneratedRegex(@"^(\d+)\.(\d+)\.(\d+).*?[\.\+](\d+)$")]
	protected static partial Regex AlliedModdersVersionRegex { get; }

	public required ServerInstallComponent Component { get; init; }

	public virtual async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		(Release release, ReleaseAsset asset)? downloadable;
		try
		{
			string? targetVersion = ctx.GetTargetVersion(Component);
			downloadable = await GetGithubDownloadableAsync(ctx, targetVersion, ct).ConfigureAwait(false);
		}
		catch (Exception e)
		{
			throw new InstallException($"Unable to read {Component} releases from GitHub", e);
		}

		if (downloadable == null)
		{
			throw new InstallException($"No downloadable archive found in {Component} GitHub releases");
		}

		string downloadFilename = downloadable.Value.asset.Name;
		string downloadUrl = downloadable.Value.asset.BrowserDownloadUrl;
		string targetArchive = Path.Join(ctx.InstallPath, downloadFilename);
		
		yield return new StatusMessage($"Downloading...\n URL: {downloadUrl}\n Target: \"{targetArchive}\"");
		
		try
		{
			await Helper.DownloadAsync(downloadUrl, targetArchive, ct).ConfigureAwait(false);
		}
		catch (Exception e)
		{
			Helper.SafeDelete(targetArchive);
			throw new InstallException($"Failed to download {Component}", e);
		}
		
		ct.ThrowIfCancellationRequested();

		try
		{
			await foreach (var msg in ExtractArchiveAsync(ctx, targetArchive, ct).ConfigureAwait(false))
			{
				yield return msg;
			}
		}
		finally
		{
			Helper.SafeDelete(targetArchive);
		}
	}

	protected virtual async IAsyncEnumerable<StatusMessage> ExtractArchiveAsync(
		ServerInstallContext ctx, string archivePath,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		yield return new StatusMessage("Extracting...");
		try
		{
			await Helper.ExtractAsync(archivePath, ctx.GameModPath, true, ct).ConfigureAwait(false);
		}
		catch (Exception e)
		{
			throw new InstallException($"Failed to extract {archivePath}", e);
		}
	}

	public virtual async Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken ct = default)
	{
		string? path = null;
		try
		{
			path = GetComponentPath(ctx);
		}
		catch (UnsetInstallPathException) {}
		
		if (path == null || !IsInstalled(path))
		{
			return ComponentInfo.ReadyToInstallWithVersionSelect;
		}
		
		string? localVersion = null;
		string? upgradeVersion = null;
		if (checkForUpgrades)
		{
			try
			{
				localVersion = GetLocalVersion(path);
				if (localVersion != null)
				{
					(Release release, ReleaseAsset asset)? downloadable = await GetGithubDownloadableAsync(ctx, false, ct)
						.ConfigureAwait(false);
					
					if (downloadable != null)
					{
						string? latestVersion = GetVersionString(downloadable.Value.release);
						if (latestVersion != null)
						{
							Trace.WriteLine($"Checking {Component} version [Local: {localVersion}, Latest: {latestVersion}]");
							if (((IServerComponentInstaller<ComponentInfo>)this)
							    .TryCompareVersions(localVersion, latestVersion, out int? cmp) && cmp < 0)
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
			UpgradeVersion = upgradeVersion,
			Version = localVersion,
			VersionSelectable = true
		};
	}

	protected abstract string GetComponentPath(ServerInstallContext ctx);

	protected virtual bool IsInstalled(string path) => Path.Exists(path);

	protected abstract string? GetLocalVersion(string path);

	protected static string? ExtractAlliedModdersVersion(string input)
	{
		Match match = AlliedModdersVersionRegex.Match(input);
		if (match.Success)
		{
			return new Version(
				int.TryParse(match.Groups[1].Value, out int major) ? major : 0,
				int.TryParse(match.Groups[2].Value, out int minor) ? minor : 0,
				int.TryParse(match.Groups[3].Value, out int build) ? build : 0,
				int.TryParse(match.Groups[4].Value, out int revision) ? revision : 0
			).ToString();
		}

		return null;
	}
}
