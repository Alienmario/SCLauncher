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
using SCLauncher.backend.util.stream;
using FileMode = System.IO.FileMode;

namespace SCLauncher.backend.install;

public class InstallHelper(HttpClient httpClient)
{
	public static TimeSpan DefaultNetworkTimeout => TimeSpan.FromSeconds(10);
	
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
		await using var fileStream = new FileStream(destination, FileMode.Create);
		await stream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
	}

	public Task DownloadAsync(string url, string destination, IProgress<int>? progress, CancellationToken ct = default)
	{
		return DownloadAsync(url, destination, DefaultNetworkTimeout, progress, ct);
	}
	
	public async Task DownloadAsync(string url, string destination, 
		TimeSpan? timeout, IProgress<int>? progress, CancellationToken ct = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
			.ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		// Try to get total file size for percentage calculation
		long? totalBytes = response.Content.Headers.ContentLength;

		var contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
		await using var responsiveStream = new ResponsiveStream(contentStream, timeout, totalBytes, progress);
		await using var fileStream = new FileStream(destination, FileMode.Create);

		await responsiveStream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
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