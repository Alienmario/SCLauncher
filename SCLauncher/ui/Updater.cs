using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using SCLauncher.backend.install;
using SCLauncher.backend.util;
using SCLauncher.model.install;

namespace SCLauncher.ui;

public class Updater(InstallHelper installHelper)
{
	private const string StagedExecutableSuffix = ".new";
	private const string FinishUpdateCommand = "finish-update";

	public async Task<List<Release>> CheckForUpdates()
	{
		if (App.RepositoryUrl == null)
		{
			Trace.WriteLine("[Updater] RepositoryUrl attribute was not packed in assembly, skipping update check");
			return [];
		}
		if (!App.RepositoryUrl.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
		{
			Trace.WriteLine("[Updater] RepositoryUrl is not a github URL, skipping update check");
			return [];
		}
		
		// Split the URL by '/' and remove empty entries caused by trailing slashes
		string[] parts = App.RepositoryUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
		string repo = parts.Last().Replace(".git", "", StringComparison.OrdinalIgnoreCase);
		string owner = parts[^2];
		
		Trace.WriteLine($"[Updater] Fetching github repository releases: {owner}/{repo}");
		var allReleases = await installHelper.GithubClient.Repository.Release.GetAll(owner, repo);
		
		var newerReleases = allReleases
			.Where(r => !r.Draft && !r.Prerelease && VersionUtils.SmartCompare(App.Version, r.TagName) < 0)
			.OrderByDescending(r => r.TagName, Comparer<string>.Create(VersionUtils.SmartCompare))
			.ToList();
		
		Trace.WriteLine($"[Updater] Current version: {App.Version}, {newerReleases.Count} newer release(s)");
		return newerReleases;
	}

	public async IAsyncEnumerable<ProgressUpdate> RunUpdate(Release release,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		string processPath = Environment.ProcessPath!;

		// Find a matching asset (archive containing the executable)
		var asset = FindAsset(release);
		if (asset == null)
		{
			throw new InstallException("No compatible download found in latest release.");
		}

		string workDir = Path.Join(Path.GetTempPath(), "SCLauncher", "Update");
		if (Directory.Exists(workDir))
			Directory.Delete(workDir, true);
		Directory.CreateDirectory(workDir);

		string archivePath = Path.Join(workDir, asset.Name);

		// Download
		yield return new ProgressUpdate { Text = $"Downloading {asset.Name}..." };
		AsyncEnumerableBuffer<int> buffer = new();
		
		Task dlTask = Task.Run(async () =>
		{
			try
			{
				await installHelper.DownloadAsync(asset.BrowserDownloadUrl, archivePath, buffer, ct)
					.ConfigureAwait(false);
			}
			catch (Exception e) when (e.Contains<TimeoutException>())
			{
				throw new InstallException("Download timed out", e);
			}
			finally
			{
				buffer.Complete();
			}
		}, ct);

		try
		{
			await foreach (int percent in buffer.ReadAllAsync(ct).ConfigureAwait(false))
			{
				yield return new ProgressUpdate { Text = $"Downloading {asset.Name} {percent,3}%" };
			}
		}
		finally
		{
			await dlTask;
		}

		// Extract
		yield return new ProgressUpdate { Text = "Extracting..." };
		string extractDir = Path.Join(workDir, "Extract");
		Directory.CreateDirectory(extractDir);
		await installHelper.ExtractAsync(archivePath, extractDir, true, ct).ConfigureAwait(false);

		// Find the new executable
		string? newExePath = Directory
			.EnumerateFiles(extractDir, "*", SearchOption.AllDirectories)
			.Select(s => new FileInfo(s))
			.OrderByDescending(info => info.Length)
			.Select(info => info.FullName)
			.FirstOrDefault();

		if (newExePath == null)
		{
			throw new InstallException("Could not find an executable in the downloaded archive.");
		}
		
		ct.ThrowIfCancellationRequested();
		
		// Stage the new executable next to the current one.
		string stagedPath = GetStagedPath(processPath);
		File.Copy(newExePath, stagedPath, true);
		if (!OperatingSystem.IsWindows())
		{
			File.SetUnixFileMode(stagedPath, File.GetUnixFileMode(processPath));
		}
		
		// Cleanup
		yield return new ProgressUpdate { Text = "Cleaning up..." };
		Directory.Delete(workDir, true);

		// Hand off: the staged executable waits for this process to exit, then replaces it and starts it.
		// Important: the running executable is never modified while in use.
		yield return new ProgressUpdate { Text = "Restarting..." };
		Process.Start(stagedPath, [FinishUpdateCommand, processPath, Environment.ProcessId.ToString()]);
		Environment.Exit(0);
	}

	/// Checks whether the app was started by <see cref="RunUpdate"/> to finish an update.
	public static bool IsFinishUpdateCommand(string[] args)
	{
		return args is [FinishUpdateCommand, _, _];
	}

	/// <summary>
	/// Second stage of the update, running from the staged executable.
	/// Waits for the previous process to exit, copies itself over the original executable and starts it.
	/// </summary>
	/// <param name="args">["finish-update", targetPath, pid]</param>
	/// <returns>Process exit code</returns>
	public static int FinishUpdate(string[] args)
	{
		string stagedPath = Environment.ProcessPath!;
		string targetPath = args[1];
		int exitCode = 0;
		
		try
		{
			using var oldProcess = Process.GetProcessById(int.Parse(args[2]));
			if (!oldProcess.WaitForExit(TimeSpan.FromSeconds(10)))
			{
				Trace.WriteLine("[Updater] Timed out waiting for the previous process to exit");
			}
		}
		catch (ArgumentException)
		{
			// Already exited
		}

		try
		{
			// Retry for a bit, in case the file is still locked by the exiting process
			for (int attempt = 1;; attempt++)
			{
				try
				{
					File.Copy(stagedPath, targetPath, true);
					break;
				}
				catch (IOException) when (attempt < 10)
				{
					Thread.Sleep(500);
				}
			}
			Trace.WriteLine($"[Updater] Replaced {targetPath}");
		}
		catch (Exception e)
		{
			// Still start the (unchanged) target, so the app doesn't just disappear
			e.Log("[Updater] Failed to replace executable");
			exitCode = 1;
		}

		Process.Start(targetPath);
		return exitCode;
	}

	/// Deletes leftover files from a previous app update.
	public static void Cleanup()
	{
		Task.Run(async () =>
		{
			try
			{
				string? processPath = Environment.ProcessPath;
				if (string.IsNullOrEmpty(processPath)) return;

				string stagedPath = GetStagedPath(processPath);
				if (!File.Exists(stagedPath)) return;

				// Retry for a bit, in case the staged process is still terminating and locking the file
				for (int attempt = 1;; attempt++)
				{
					try
					{
						File.Delete(stagedPath);
						break;
					}
					catch (Exception e) when (e is IOException or UnauthorizedAccessException && attempt < 20)
					{
						await Task.Delay(500);
					}
				}
				Trace.WriteLine($"[Updater] Deleted leftover file: {stagedPath}");
			}
			catch (Exception e)
			{
				e.Log("[Updater] Failed to delete leftover file");
			}
		});
	}

	/// Path of the staged new executable, e.g. SCLauncher.new.exe / SCLauncher.new
	private static string GetStagedPath(string processPath)
	{
		return Path.Join(
			Path.GetDirectoryName(processPath),
			Path.GetFileNameWithoutExtension(processPath) + StagedExecutableSuffix + Path.GetExtension(processPath));
	}

	private static ReleaseAsset? FindAsset(Release release)
	{
		foreach (var asset in release.Assets)
		{
			string name = asset.Name.ToLowerInvariant();
			if (!name.EndsWith(".zip") && !name.EndsWith(".tar.gz") && !name.EndsWith(".tgz"))
				continue;

			if (OperatingSystem.IsWindows() && name.Contains("win")
			    || OperatingSystem.IsLinux() && name.Contains("linux"))
				return asset;
		}
		return null;
	}
}