using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using Octokit;
using SCLauncher.Shared.Model.ServerInstall;
using SCLauncher.Shared.Service;

namespace SCLauncher.Backend.ServerInstall.Components;

internal partial class MetaModInstaller : BaseGithubServerComponentInstaller
{
	[GeneratedRegex(@"^mmsource.*-windows.*\.(?:tar\.gz|tgz|zip)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex AssetRegexWindows { get; }
	
	[GeneratedRegex(@"^mmsource.*-linux.*\.(?:tar\.gz|tgz|zip)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex AssetRegexLinux { get; }
	
	[SetsRequiredMembers]
	public MetaModInstaller(IInstallHelperService helper) : base(helper)
	{
		Component = ServerInstallComponent.MetaMod;
		GithubOwner = "alliedmodders";
		GithubRepo = "metamod-source";
		
		if (OperatingSystem.IsWindows())
			GithubAssetRegex = AssetRegexWindows;
		else if (OperatingSystem.IsLinux())
			GithubAssetRegex = AssetRegexLinux;
		else throw new PlatformNotSupportedException();
	}

	protected override string GetComponentPath(ServerInstallContext ctx) =>
		Path.Join(ctx.AddonsPath, "metamod");

	protected override string? GetLocalVersion(string path)
	{
		if (OperatingSystem.IsWindows())
		{
			string dll = Path.Join(path, "bin", "server.dll");
			string? productVersion = Helper.GetFileProductVersion(dll);
			if (productVersion != null)
			{
				return ExtractAlliedModdersVersion(productVersion);
			}
		}
		else if (OperatingSystem.IsLinux())
		{
			// wat do
		}
		else throw new PlatformNotSupportedException();

		return null;
	}

	public override string? GetVersionString(Release release) => ExtractAlliedModdersVersion(release.TagName);
}
