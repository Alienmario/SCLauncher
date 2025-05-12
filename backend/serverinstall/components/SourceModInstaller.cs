using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.backend.install;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

public class SourceModInstaller(InstallHelper helper) : IServerComponentInstaller<ComponentInfo>
{
	
	private const string SourceModVersion = "1.12";

	public ServerInstallComponent ComponentType => ServerInstallComponent.SourceMod;
	
	public async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		(string url, string filename) dl = await GetDownloadAsync(SourceModVersion, cancellationToken);
		string archivePath = Path.Join(ctx.InstallDir, dl.filename);
		
		yield return new StatusMessage($"Downloading...\n URL: {dl.url}\n Target: \"{archivePath}\"");
		
		try
		{
			await helper.DownloadAsync(dl.url, archivePath, cancellationToken);
		}
		catch (Exception e)
		{
			helper.SafeDelete(archivePath);
			throw new InstallException("Failed to download SourceMod", e);
		}

		yield return new StatusMessage($"Extracting...");
		
		try
		{
			await helper.ExtractAsync(archivePath, ctx.GameModDir, true, cancellationToken);
		}
		catch (Exception e)
		{
			throw new InstallException("Failed to extract SourceMod", e);
		}
		finally
		{
			helper.SafeDelete(archivePath);
		}
	}

	public Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken cancellationToken = default)
	{
		ComponentInfo info = ComponentInfo.ReadyToInstall;
		
		string sourcemod = Path.Join(ctx.AddonsDir, "sourcemod");
		if (Directory.Exists(sourcemod))
		{
			info = new ComponentInfo { Path = sourcemod };
		}
		
		return Task.FromResult(info);
	}

	private async Task<(string url, string filename)> GetDownloadAsync(string version,
		CancellationToken cancellationToken = default)
	{
		string platform = Environment.OSVersion.Platform switch
		{
			PlatformID.Win32NT => "windows",
			PlatformID.Unix => "linux",
			_ => throw new PlatformNotSupportedException()
		};
		string baseUrl = $"https://sm.alliedmods.net/smdrop/{version}/";
		string filename = await helper.HttpClient.GetStringAsync($"{baseUrl}sourcemod-latest-{platform}", cancellationToken);
		return (baseUrl + filename, filename);
	}

}