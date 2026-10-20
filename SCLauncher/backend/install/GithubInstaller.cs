using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

namespace SCLauncher.backend.install;

internal class GithubInstaller<TContext>(InstallHelper helper)
{
	public required string GithubOwner { get; init; }
	public required string GithubRepo { get; init; }
	public Regex? GithubAssetRegex { get; init; }
	public Func<Match, TContext, bool> GithubAssetRegexEvaluator { get; init; } = (match, ctx) => match.Success;

	/// Non-prerelease only
	public virtual async Task<Release?> GetLatestGithubReleaseAsync()
	{
		try
		{
			return await helper.GithubClient.Repository.Release.GetLatest(GithubOwner, GithubRepo)
				.ConfigureAwait(false);
		}
		catch (NotFoundException)
		{
			return null;
		}
	}
	
	public virtual async Task<Release?> GetGithubReleaseByTagAsync(string tag)
	{
		try
		{
			return await helper.GithubClient.Repository.Release.Get(GithubOwner, GithubRepo, tag)
				.ConfigureAwait(false);
		}
		catch (NotFoundException)
		{
			return null;
		}
	}
	
	public virtual Task<IReadOnlyList<Release>> GetAllGithubReleasesAsync()
	{
		return helper.GithubClient.Repository.Release.GetAll(GithubOwner, GithubRepo);
	}
	public virtual bool TryGetDownloadableAsset(Release release, TContext context,
		[NotNullWhen(true)] out ReleaseAsset? matchedAsset)
	{
		if (GithubAssetRegex == null)
			throw new NotImplementedException($"{nameof(GithubAssetRegex)} not provided");
		
		foreach (ReleaseAsset asset in release.Assets)
		{
			Match match = GithubAssetRegex.Match(asset.Name);
			if (GithubAssetRegexEvaluator(match, context))
			{
				matchedAsset = asset;
				return true;
			}
		}
		matchedAsset = null;
		return false;
	}

	public virtual async Task<(Release, ReleaseAsset)?> GetGithubDownloadableAsync(
		bool preRelease, TContext ctx, CancellationToken ct = default)
	{
		// optimized fast path
		if (!preRelease)
		{
			Release? latestRelease = await GetLatestGithubReleaseAsync().ConfigureAwait(false);
			if (latestRelease == null)
			{
				return null;
			}
			if (TryGetDownloadableAsset(latestRelease, ctx, out var asset))
			{
				return (latestRelease, asset);
			}
		}
		
		IEnumerable<Release> releases = (await GetAllGithubReleasesAsync().ConfigureAwait(false))
			.Where(r => r.Prerelease == preRelease);
		
		ct.ThrowIfCancellationRequested();

		foreach (var release in releases)
		{
			if (TryGetDownloadableAsset(release, ctx, out var asset))
			{
				return (release, asset);
			}
		}
		return null;
	}
	
	public virtual async Task<(Release, ReleaseAsset)?> GetGithubDownloadableAsync(
		string tag, TContext ctx, CancellationToken ct = default)
	{
		Release? release = await GetGithubReleaseByTagAsync(tag).ConfigureAwait(false);
		if (release != null && TryGetDownloadableAsset(release, ctx, out var asset))
		{
			return (release, asset);
		}
		return null;
	}
	
}