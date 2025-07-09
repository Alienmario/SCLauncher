using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

public partial class MetaModInstaller(InstallHelper helper) : IServerComponentInstaller<ComponentInfo>
{
	private const string MetaModVersion = "1.12";

	public ServerInstallComponent ComponentType => ServerInstallComponent.MetaMod;
	
	public async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken ct)
	{
		(string url, string filename) dl = await GetDownloadAsync(MetaModVersion, ct);
		string archivePath = Path.Join(ctx.InstallDir, dl.filename);
		
		yield return new StatusMessage($"Downloading...\n URL: {dl.url}\n Target: \"{archivePath}\"");
		
		try
		{
			await helper.DownloadAsync(dl.url, archivePath, ct);
		}
		catch (Exception e)
		{
			helper.SafeDelete(archivePath);
			throw new InstallException("Failed to download MetaMod", e);
		}

		yield return new StatusMessage($"Extracting...");

		try
		{
			await helper.ExtractAsync(archivePath, ctx.GameModDir, true, ct);
		}
		catch (Exception e)
		{
			throw new InstallException("Failed to extract MetaMod", e);
		}
		finally
		{
			helper.SafeDelete(archivePath);
		}
	}

	public async Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken ct = default)
	{
		string metamod = Path.Join(ctx.AddonsDir, "metamod");
		
		if (!Directory.Exists(metamod))
		{
			return ComponentInfo.ReadyToInstall;
		}
		
		Version? localVersion = null;
		Version? upgradeVersion = null;
		if (checkForUpgrades)
		{
			try
			{
				localVersion = GetLocalVersion(metamod);
				if (localVersion != null)
				{
					Version? latestVersion = await GetLatestVersionAsync(MetaModVersion, ct);
					if (latestVersion != null)
					{
						Trace.WriteLine($"Checking MetaMod version [Local: {localVersion}, Latest: {latestVersion}]");
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
			Path = metamod,
			Upgradable = upgradeVersion != null,
			UpgradeVersion = upgradeVersion?.ToString(),
			Version = localVersion?.ToString()
		};
	}

	private async Task<(string url, string filename)> GetDownloadAsync(string baseVersion,
		CancellationToken cancellationToken = default)
	{
		string platform = Environment.OSVersion.Platform switch
		{
			PlatformID.Win32NT => "windows",
			PlatformID.Unix => "linux",
			_ => throw new PlatformNotSupportedException()
		};
		string baseUrl = $"https://mms.alliedmods.net/mmsdrop/{baseVersion}/";
		string filename = await helper.HttpClient.GetStringAsync($"{baseUrl}mmsource-latest-{platform}", cancellationToken);
		return (baseUrl + filename, filename);
	}

	[GeneratedRegex(@"^(\d+)\.(\d+)\..*\+(\d+)$")]
	private static partial Regex ProductVersionRegex();
	
	private Version? GetLocalVersion(string metamod)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			string metamodDll = Path.Join(metamod, "bin", "server.dll");
			string? productVersion = helper.GetFileProductVersion(metamodDll);
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

	[GeneratedRegex(@"^mmsource-(\d+)\.(\d+)\..*-git(\d+)")]
	private static partial Regex DlFilenameVersionRegex();
	
	private async Task<Version?> GetLatestVersionAsync(string baseVersion, CancellationToken cancellationToken = default)
	{
		var dl = await GetDownloadAsync(baseVersion, cancellationToken);
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