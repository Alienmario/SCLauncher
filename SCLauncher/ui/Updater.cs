using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using SCLauncher.backend.install;
using SCLauncher.backend.util;
using SCLauncher.model.install;

namespace SCLauncher.ui;

public class Updater(InstallHelper installHelper)
{
	private const string BackupExecutableSuffix = ".old";

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
		await installHelper.DownloadAsync(asset.BrowserDownloadUrl, archivePath, ct);

		// Extract
		yield return new ProgressUpdate { Text = "Extracting..." };
		string extractDir = Path.Join(workDir, "Extract");
		Directory.CreateDirectory(extractDir);
		await installHelper.ExtractAsync(archivePath, extractDir, true, ct);

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
		
		// Replace: rename current exe to .old, then copy new one
		yield return new ProgressUpdate { Text = "Replacing executable..." };
		File.Move(processPath, processPath + BackupExecutableSuffix, true);
		File.Copy(newExePath, processPath, true);
		
		// Cleanup
		yield return new ProgressUpdate { Text = "Cleaning up..." };
		Directory.Delete(workDir, true);

		// Restart
		yield return new ProgressUpdate { Text = "Restarting..." };
		Process.Start(processPath);
		Environment.Exit(0);
	}

	/// Deletes old executable backup created by app update.
	public static void CleanupExecutableBackup()
	{
		Task.Run(async () =>
		{
			try
			{
				string? processPath = Environment.ProcessPath;
				if (string.IsNullOrEmpty(processPath)) return;

				string oldPath = processPath + BackupExecutableSuffix;
				if (File.Exists(oldPath))
				{
					// Wait a moment in case the old process is still terminating and locking the file
					// Also helps in case the updated app crashes.
					await Task.Delay(1000);
					File.Delete(oldPath);
					Trace.WriteLine($"Deleted old executable: {oldPath}");
				}
			}
			catch (Exception e)
			{
				e.Log("Failed to cleanup .old executable");
			}
		});
	}

	private static ReleaseAsset? FindAsset(Release release)
	{
		bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

		foreach (var asset in release.Assets)
		{
			string name = asset.Name.ToLowerInvariant();
			if (!name.EndsWith(".zip") && !name.EndsWith(".tar.gz") && !name.EndsWith(".tgz"))
				continue;

			if (isWindows && name.Contains("win") || isLinux && name.Contains("linux"))
				return asset;
		}

		// Fallback: if there's only one archive, use it
		var archives = release.Assets.Where(a =>
		{
			string n = a.Name.ToLowerInvariant();
			return n.EndsWith(".zip") || n.EndsWith(".tar.gz") || n.EndsWith(".tgz");
		}).ToList();

		return archives.Count == 1 ? archives[0] : null;
	}
}