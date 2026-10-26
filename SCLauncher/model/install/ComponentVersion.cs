using System;

namespace SCLauncher.model.install;

public class ComponentVersion
{
	/// Identifier used by the installer (GitHub release tag or Steam branch name).
	public required string Version { get; init; }

	/// Display title for the overlay header (e.g., release name/tag or branch name).
	public string Title { get; init; } = "";

	/// Markdown or text description (release notes or branch description + build ID).
	public string? Description { get; init; }

	/// Optional external URL (e.g., GitHub release page).
	public string? Url { get; init; }

	/// Release publication timestamp or branch last-updated timestamp.
	public DateTimeOffset? Date { get; init; }

	/// Whether this version is a pre-release.
	public bool Prerelease { get; init; }
}
