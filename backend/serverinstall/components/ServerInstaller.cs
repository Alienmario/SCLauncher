using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
		[EnumeratorCancellation] CancellationToken cancellationToken)
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
			await foreach (var message in InstallViaDepotDownloaderApi(ctx, cancellationToken))
			{
				yield return message;
			}
		}
	}

	public static async IAsyncEnumerable<StatusMessage> InstallViaSteamClient(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		SteamUtils.InstallApp(ctx.Params.AppId);
		yield return new StatusMessage("Waiting for Steam to finish downloading");
	}

	public static async IAsyncEnumerable<StatusMessage> InstallViaDepotDownloaderApi(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		Exception? exception = null;
		bool completed = false;
		using BlockingCollection<StatusMessage> messages = new();
		using ConsoleMessageRewriter consoleRewriter = new(messages);

		DownloadCallbacks callbacks = new DownloadCallbacks
		{
			DepotDownloadComplete = complete =>
			{
				completed = true;
			}
		};
		
		Task dlTask = Task.Run(async () =>
		{
			ContentDownloader.Config.InstallDirectory = ctx.ServerFolder;
			ContentDownloader.Config.VerifyAll = true;

			await Task.Run(() =>
			{
				ContentDownloader.InitializeSteam3(null, null);
			}, cancellationToken).WaitAsync(TimeSpan.FromSeconds(7), cancellationToken);
			
			await ContentDownloader.DownloadAppAsync(
				(uint) ctx.Params.AppId, 
				[],
				ContentDownloader.DEFAULT_BRANCH,
				null, null, null, false, false, callbacks);
			
		}, cancellationToken).ContinueWith(t => exception = t.Exception, cancellationToken);

		while (true)
		{
			Task<StatusMessage> msgTakeTask = Task.Run(() => messages.Take(cancellationToken), cancellationToken);
			await Task.WhenAny(dlTask, msgTakeTask);
		
			if (msgTakeTask.IsCompletedSuccessfully)
			{
				yield return msgTakeTask.Result;
				
				if (msgTakeTask.Result.Status == MessageStatus.Error)
				{
					break;
				}
			}

			if (dlTask.IsCompleted)
			{
				break;
			}
		
			if (cancellationToken.IsCancellationRequested)
				break;
		}

		if (exception != null)
		{
			Trace.WriteLine(exception);
			yield return new StatusMessage("Download failed\n" + exception, MessageStatus.Error);
		}
		else if (!completed && !cancellationToken.IsCancellationRequested)
		{
			yield return new StatusMessage("Download failed", MessageStatus.Error);
		}
		
		ContentDownloader.ShutdownSteam3();
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