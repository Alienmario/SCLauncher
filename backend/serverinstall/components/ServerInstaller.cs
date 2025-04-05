using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DepotDownloader;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

public class ServerInstaller : IServerComponentInstaller<ComponentInfo>
{
	
	public ServerInstallComponent Type => ServerInstallComponent.Server;
	
	public async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (ctx.Params.Method == ServerInstallMethod.Steam)
		{
			await foreach (var message in InstallViaSteamClient(ctx, cancellationToken))
			{
				yield return message;
			}
		}
		else
		{
			await foreach (var message in InstallViaDepotDownloader(ctx, cancellationToken))
			{
				yield return message;
			}
		}
	}

	public static async IAsyncEnumerable<StatusMessage> InstallViaSteamClient(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		SteamUtils.InstallApp(ctx.Params.AppId);
		yield return new StatusMessage("Waiting for Steam to finish downloading");
	}

	private static async IAsyncEnumerable<StatusMessage> InstallViaDepotDownloader(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var cfg = new AppDownloadConfig
		{
			InstallDirectory = ctx.ServerFolder,
			VerifyAll = true,
			AppId = (uint) ctx.Params.AppId
		};

		int? exitCode = null;
		await foreach ((string message, bool error) in SubProcess.AppDownload(cfg, i => exitCode = i, cancellationToken))
		{
			yield return new StatusMessage(message);
		}

		if (exitCode != SubProcess.Success)
		{
			yield return new StatusMessage("Download failed", MessageStatus.Error);
		}
	}
	
	public Task<ComponentInfo?> GatherInfo(ServerInstallContext ctx)
	{
		return Task.FromResult<ComponentInfo?>(null);
	}

	public Task<bool> ShouldInstall(ServerInstallContext ctx, ComponentInfo? installationInfo)
	{
		return Task.FromResult(true);
	}

}