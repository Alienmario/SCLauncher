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
using Path = System.IO.Path;

namespace SCLauncher.backend.serverinstall.components;

public class SourceCoopInstaller(InstallHelper helper) : IServerComponentInstaller<ComponentInfo>
{
	
	public ServerInstallComponent ComponentType => ServerInstallComponent.SourceCoop;
	
	public async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		Release release;
		try
		{
			release = await GetLatestRelease();
		}
		catch (Exception e)
		{
			throw new InstallException("Unable to find latest SourceCoop release on GitHub", e);
		}

		(string url, string filename) dl = GetDownload(ctx, release);
		string archivePath = Path.Join(ctx.InstallDir, dl.filename);
		
		yield return new StatusMessage($"Downloading...\n URL: {dl.url}\n Target: \"{archivePath}\"");
		
		try
		{
			await helper.DownloadAsync(dl.url, archivePath, cancellationToken);
		}
		catch (Exception e)
		{
			helper.SafeDelete(archivePath);
			throw new InstallException("Failed to download SourceCoop", e);
		}
		
		yield return new StatusMessage($"Extracting...");
		
		try
		{
			await helper.ExtractAsync(archivePath, ctx.GameModDir, true, cancellationToken);
		}
		catch (Exception e)
		{
			throw new InstallException("Failed to extract SourceCoop", e);
		}
		finally
		{
			helper.SafeDelete(archivePath);
		}
	}
	
	public async Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken cancellationToken = default)
	{
		string scPlugin = Path.Join(ctx.AddonsDir, "sourcemod", "plugins", "srccoop.smx");
		if (!File.Exists(scPlugin))
		{
			return ComponentInfo.ReadyToInstall;
		}

		string? localVersion = null;
		string? upgradeVersion = null;
		if (checkForUpgrades)
		{
			try
			{
				var pluginInfo = SourcemodPluginAnalyzer.Analyze(scPlugin);
				localVersion = pluginInfo.MyInfo.Version;
				if (localVersion != null)
				{
					if (!localVersion.StartsWith('v'))
						localVersion = "v" + localVersion;
					
					Release release = await GetLatestRelease();
					string releaseVersion = release.TagName;
					
					Trace.WriteLine($"Checking SourceCoop version [Local: {localVersion}, Release: {releaseVersion}]");
					if (VersionUtils.SmartCompare(localVersion, releaseVersion) < 0)
					{
						upgradeVersion = releaseVersion;
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
			Path = scPlugin,
			Upgradable = upgradeVersion != null,
			UpgradeVersion = upgradeVersion,
			Version = localVersion
		};
	}

	private Task<Release> GetLatestRelease()
	{
		return helper.GithubClient.Repository.Release.GetLatest("ampreeT", "SourceCoop");
	}

	private static (string url, string filename) GetDownload(ServerInstallContext ctx, Release release)
	{
		foreach (ReleaseAsset asset in release.Assets)
		{
			if (asset.Name.EndsWith($"-{ctx.Params.AppInfo.ModFolder}.zip"))
			{
				return (asset.BrowserDownloadUrl, asset.Name);
			}
		}

		throw new InstallException($"No downloadable archive for game \"{ctx.Params.AppInfo.ModFolder}\" in " +
		                           $"Github release \"{release.Name}\"");
	}

}