using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SCLauncher.backend.install;
using SCLauncher.backend.util;

namespace SCLauncher.backend.serverinstall.components;

internal partial class SourceModPluginInstaller : BaseGithubServerComponentInstaller
{
	[GeneratedRegex(@"^(?:[-_.]*(?<token>[^-_.]+))+\.(?:tar\.gz|tgz|zip)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex AssetRegex { get; }

	public required string PluginFileName { get; init; }
	
	protected SourceModPluginInstaller(InstallHelper helper) : base(helper)
	{
		GithubAssetRegex = AssetRegex;
		GithubAssetRegexEvaluator = (match, ctx) =>
		{
			// by default, at least 1 delimited token must match the mod folder
			return match.Groups["token"].Captures.Any(capture =>
				capture.Value.Equals(ctx.Params.Profile.ModFolder, StringComparison.OrdinalIgnoreCase));
		};
	}

	protected override string GetComponentPath(ServerInstallContext ctx) =>
		Path.Join(ctx.AddonsPath, "sourcemod", "plugins", PluginFileName);

	protected override string? GetLocalVersion(string path) =>
		SourcemodPluginAnalyzer.Analyze(path).MyInfo.Version;
}
