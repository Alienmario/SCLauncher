using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

namespace SCLauncher.Shared.Service;

public interface IInstallHelperService
{
	HttpClient HttpClient { get; }

	GitHubClient GithubClient { get; }

	Task ExtractAsync(string archive, string destination, bool overwriteFiles,
		CancellationToken ct = default);

	Task DownloadAsync(string url, string destination, CancellationToken ct = default);

	Task DownloadAsync(string url, string destination, IProgress<int>? progress, CancellationToken ct = default);

	Task DownloadAsync(string url, string destination,
		TimeSpan? timeout, IProgress<int>? progress, CancellationToken ct = default);

	bool SafeDelete(string? path);

	string? GetFileProductVersion(string filename);
}
