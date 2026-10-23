using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using SCLauncher.model.exception;
using SCLauncher.model.install;

namespace SCLauncher.backend.install;

using GithubDownloadable = (Release release, ReleaseAsset asset);

internal class GithubInstaller<TContext>(InstallHelper helper)
{
	public required string GithubOwner { get; init; }
	public required string GithubRepo { get; init; }
	public Regex? GithubAssetRegex { get; init; }
	public Func<Match, TContext, bool> GithubAssetRegexEvaluator { get; init; } = (match, ctx) => match.Success;

	protected InstallHelper Helper => helper;

	/// Returns non-prerelease only
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
		catch (RateLimitExceededException e)
		{
			throw new ApiRateLimitException(e.Reset);
		}
		catch (SecondaryRateLimitExceededException)
		{
			throw new ApiRateLimitException();
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
		catch (RateLimitExceededException e)
		{
			throw new ApiRateLimitException(e.Reset);
		}
		catch (SecondaryRateLimitExceededException)
		{
			throw new ApiRateLimitException();
		}
	}
	
	public virtual async Task<IReadOnlyList<Release>> GetAllGithubReleasesAsync()
	{
		try
		{
			return await helper.GithubClient.Repository.Release.GetAll(GithubOwner, GithubRepo)
				.ConfigureAwait(false);
		}
		catch (RateLimitExceededException e)
		{
			throw new ApiRateLimitException(e.Reset);
		}
		catch (SecondaryRateLimitExceededException)
		{
			throw new ApiRateLimitException();
		}
	}

	public virtual bool TryGetAsset(Release release, TContext context, [NotNullWhen(true)] out ReleaseAsset? matchedAsset)
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

	public virtual async Task<IReadOnlyList<GithubDownloadable>> GetGithubDownloadablesAsync(
		TContext ctx, bool? preRelease = null,
		CancellationToken ct = default)
	{
		var releases = await GetAllGithubReleasesAsync().ConfigureAwait(false);
		ct.ThrowIfCancellationRequested();

		var result = new List<GithubDownloadable>();
		foreach (var release in releases)
		{
			if (release.Draft || (preRelease != null && preRelease != release.Prerelease))
				continue;
			
			if (TryGetAsset(release, ctx, out var asset))
			{
				result.Add((release, asset));
			}
		}
		return result;
	}

	public virtual async Task<GithubDownloadable?> GetGithubDownloadableAsync(
		TContext ctx, bool? preRelease = null,
		CancellationToken ct = default)
	{
		// optimized fast path
		if (preRelease == false)
		{
			Release? latestRelease = await GetLatestGithubReleaseAsync().ConfigureAwait(false);
			if (latestRelease == null)
			{
				return null;
			}
			if (TryGetAsset(latestRelease, ctx, out var asset))
			{
				return (latestRelease, asset);
			}
		}

		// iteration path
		IEnumerable<Release> releases = await GetAllGithubReleasesAsync().ConfigureAwait(false);
		ct.ThrowIfCancellationRequested();

		foreach (var release in releases)
		{
			if (release.Draft || (preRelease != null && preRelease != release.Prerelease))
				continue;
			
			if (TryGetAsset(release, ctx, out var asset))
			{
				return (release, asset);
			}
		}
		return null;
	}

	public virtual async Task<GithubDownloadable?> GetGithubDownloadableAsync(
		TContext ctx, string? tag,
		CancellationToken ct = default)
	{
		if (tag == null) return await GetGithubDownloadableAsync(ctx, false, ct).ConfigureAwait(false);
		
		Release? release = await GetGithubReleaseByTagAsync(tag).ConfigureAwait(false);
		if (release != null && TryGetAsset(release, ctx, out var asset))
		{
			return (release, asset);
		}
		return null;
	}

	public virtual async Task<IReadOnlyList<ComponentVersion>> GatherVersionsAsync(
		TContext ctx, CancellationToken ct = default)
	{
		var downloadables = await GetGithubDownloadablesAsync(ctx, preRelease: null, ct).ConfigureAwait(false);
		return downloadables.Select(d => new ComponentVersion
		{
			Version = d.release.TagName,
			Title = string.IsNullOrWhiteSpace(d.release.Name) ? d.release.TagName : d.release.Name,
			Description = string.IsNullOrWhiteSpace(d.release.Body) ? null : d.release.Body.Trim(),
			Url = d.release.HtmlUrl,
			Date = d.release.PublishedAt ?? d.release.CreatedAt,
			Prerelease = d.release.Prerelease
		}).ToList();
	}
	
}
