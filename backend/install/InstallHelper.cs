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
	
	public GitHubClient GithubClient => new GitHubClient(new ProductHeaderValue("SourceCoopLauncher"));

	public async Task ExtractAsync(string archive, string destination, bool overwriteFiles,
		CancellationToken cancellationToken = default)
	{
		if (archive.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
		{
			ZipFile.ExtractToDirectory(archive, destination, overwriteFiles);
		}
		else if (archive.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
		         || archive.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
		{
			await using var fileStream = new FileStream(archive, FileMode.Open, FileAccess.Read);
			await using var decompressedStream = new GZipStream(fileStream, CompressionMode.Decompress);
			await TarFile.ExtractToDirectoryAsync(decompressedStream, destination, overwriteFiles, cancellationToken);
		}
		else
		{
			throw new NotSupportedException($"Unsupported archive format: \"{archive}\"");
		}
	}
	
	public async Task DownloadAsync(string url, string destination, CancellationToken cancellationToken = default)
	{
		await using Stream stream = await httpClient.GetStreamAsync(url, cancellationToken);
		await using var fileStream = new FileStream(destination, FileMode.OpenOrCreate);
		await stream.CopyToAsync(fileStream, cancellationToken);
	}

	public void SafeDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (Exception e)
		{
			Trace.WriteLine($"Failed to delete file: {path}\nException: {e}");
		}
	}

	public string? GetProductVersion(string filename)
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