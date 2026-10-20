using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using SCLauncher.backend.install;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

internal partial class BasePluginInstaller : GithubInstaller<ServerInstallContext>, IServerComponentInstaller<ComponentInfo>
{
	[GeneratedRegex(@"^(?:[-_.]*(?<token>[^-_.]+))+\.(?:tar\.gz|tgz|zip)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex AssetRegex { get; }
	
	public required ServerInstallComponent Component { get; init; }

	public required string PluginFileName { get; init; }

	private readonly InstallHelper _helper;
	
	public BasePluginInstaller(InstallHelper helper) : base(helper)
	{
		_helper = helper;
		GithubAssetRegex = AssetRegex;
		GithubAssetRegexEvaluator = (match, ctx) =>
		{
			// by default, at least 1 delimited token must match the mod folder
			return match.Groups["token"].Captures.Any(capture =>
				capture.Value.Equals(ctx.Params.Profile.ModFolder, StringComparison.OrdinalIgnoreCase));
		};
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
			throw new InstallException("No downloadable found in github releases");
		}
		
		string dlFilename = entry.Value.asset.Name;
		string dlUrl = entry.Value.asset.BrowserDownloadUrl;
		string archivePath = Path.Join(ctx.InstallPath, dlFilename);
		
		yield return new StatusMessage($"Downloading...\n URL: {dlUrl}\n Target: \"{archivePath}\"");
		
		try
		{
			await _helper.DownloadAsync(dlUrl, archivePath, ct);
		}
		catch (Exception e)
		{
			_helper.SafeDelete(archivePath);
			throw new InstallException($"Failed to download {Component}", e);
		}
		
		yield return new StatusMessage($"Extracting...");
		
		try
		{
			await _helper.ExtractAsync(archivePath, ctx.GameModPath, true, ct);
		}
		catch (Exception e)
		{
			throw new InstallException($"Failed to extract {Component}", e);
		}
		finally
		{
			_helper.SafeDelete(archivePath);
		}
	}
	
	public async Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken ct = default)
	{
		string? pluginPath = null;
		try
		{
			pluginPath = Path.Join(ctx.AddonsPath, "sourcemod", "plugins", PluginFileName);
		}
		catch (UnsetInstallPathException) {}
		
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
					(Release release, ReleaseAsset asset)? entry = await GetGithubDownloadableAsync(false, ctx, ct)
						.ConfigureAwait(false);
					if (entry != null)
					{
						string latestVersion = entry.Value.release.TagName;

						if (latestVersion.StartsWith('v'))
							latestVersion = latestVersion[1..];

						Trace.WriteLine($"Checking {Component} version [Local: {localVersion}, Latest: {latestVersion}]");
						if (VersionUtils.SmartCompare(localVersion, latestVersion) < 0)
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
			Path = pluginPath,
			Upgradable = upgradeVersion != null,
			UpgradeVersion = upgradeVersion,
			Version = localVersion
		};
	}

}