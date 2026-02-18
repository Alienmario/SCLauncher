using System;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using FileMode = System.IO.FileMode;

namespace SCLauncher.backend.install;

public class InstallHelper(HttpClient httpClient)
{
	public HttpClient HttpClient => httpClient;
	
	public GitHubClient GithubClient => new GitHubClient(new ProductHeaderValue("SCLauncher"));

	public async Task ExtractAsync(string archive, string destination, bool overwriteFiles,
		CancellationToken ct = default)
	{
		if (archive.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
		{
			await Task.Run(() => ZipFile.ExtractToDirectory(archive, destination, overwriteFiles), ct);
		}
		else if (archive.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
		         || archive.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
		{
			await using var fileStream = new FileStream(archive, FileMode.Open, FileAccess.Read);
			await using var decompressedStream = new GZipStream(fileStream, CompressionMode.Decompress);
			await TarFile.ExtractToDirectoryAsync(decompressedStream, destination, overwriteFiles, ct);
		}
		else
		{
			throw new NotSupportedException($"Unsupported archive format: \"{archive}\"");
		}
	}
	
	public async Task DownloadAsync(string url, string destination, CancellationToken ct = default)
	{
		await using Stream stream = await httpClient.GetStreamAsync(url, ct);
		await using var fileStream = new FileStream(destination, FileMode.OpenOrCreate);
		await stream.CopyToAsync(fileStream, ct);
	}

	public bool SafeDelete(string? path)
	{
		if (path == null)
			return false;
		
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
				return true;
			}
		}
		catch (Exception e)
		{
			Trace.WriteLine($"Failed to delete file: {path}\nException: {e}");
		}
		return false;
	}

	public string? GetFileProductVersion(string filename)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			try
			{
				var versionInfo = FileVersionInfo.GetVersionInfo(filename);
				return versionInfo.ProductVersion;
			}
			catch (FileNotFoundException)
			{
				return null;
			}
		}
		throw new PlatformNotSupportedException();
	}
	
}