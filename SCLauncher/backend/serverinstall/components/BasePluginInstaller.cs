using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using SCLauncher.backend.install;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

public class BasePluginInstaller(InstallHelper helper) : IServerComponentInstaller<ComponentInfo>
{

	public required ServerInstallComponent Component { get; init; }
	public required string PluginFileName { get; init; }
	public required string GithubOwner { get; init; }
	public required string GithubRepo { get; init; }

	public async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken ct)
	{
		Release release;
		try
		{
			release = await GetLatestRelease();
		}
		catch (Exception e)
		{
			throw new InstallException($"Unable to find latest {Component} release on GitHub", e);
		}

		(string url, string filename) dl = GetDownload(ctx, release);
		string archivePath = Path.Join(ctx.InstallDir, dl.filename);
		
		yield return new StatusMessage($"Downloading...\n URL: {dl.url}\n Target: \"{archivePath}\"");
		
		try
		{
			await helper.DownloadAsync(dl.url, archivePath, ct);
		}
		catch (Exception e)
		{
			helper.SafeDelete(archivePath);
			throw new InstallException($"Failed to download {Component}", e);
		}
		
		yield return new StatusMessage($"Extracting...");
		
		try
		{
			await helper.ExtractAsync(archivePath, ctx.GameModDir, true, ct);
		}
		catch (Exception e)
		{
			throw new InstallException($"Failed to extract {Component}", e);
		}
		finally
		{
			helper.SafeDelete(archivePath);
		}
	}
	
	public async Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades, CancellationToken ct = default)
	{
		string pluginPath = Path.Join(ctx.AddonsDir, "sourcemod", "plugins", PluginFileName);
		if (!File.Exists(pluginPath))
		{
			return ComponentInfo.ReadyToInstall;
		}

		string? localVersion = null;
		string? upgradeVersion = null;
		if (checkForUpgrades)
		{
			try
			{
				var pluginInfo = SourcemodPluginAnalyzer.Analyze(pluginPath);
				localVersion = pluginInfo.MyInfo.Version;
				if (localVersion != null)
				{
					Release release = await GetLatestRelease();
					string latestVersion = release.TagName;
					
					if (latestVersion.StartsWith('v'))
						latestVersion = latestVersion[1..];
					
					Trace.WriteLine($"Checking {Component} version [Local: {localVersion}, Latest: {latestVersion}]");
					if (VersionUtils.SmartCompare(localVersion, latestVersion) < 0)
					{
						upgradeVersion = latestVersion;
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
			Path = pluginPath,
			Upgradable = upgradeVersion != null,
			UpgradeVersion = upgradeVersion,
			Version = localVersion
		};
	}

	private Task<Release> GetLatestRelease()
	{
		return helper.GithubClient.Repository.Release.GetLatest(GithubOwner, GithubRepo);
	}

	private static (string url, string filename) GetDownload(ServerInstallContext ctx, Release release)
	{
		foreach (ReleaseAsset asset in release.Assets)
		{
			if (asset.Name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
			    || asset.Name.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)
			    || asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
			{
				foreach (string split in asset.Name.Split(['-', '_', '.'], 
					         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					if (split.Equals(ctx.Params.AppInfo.ModFolder, StringComparison.OrdinalIgnoreCase))
					{
						return (asset.BrowserDownloadUrl, asset.Name);
					}
				}
			}
		}

		throw new InstallException($"No downloadable archive for game \"{ctx.Params.AppInfo.ModFolder}\" in " +
		                           $"Github release \"{release.Name}\"");
	}

}