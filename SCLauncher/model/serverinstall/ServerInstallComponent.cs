using System;
using System.Collections.Immutable;
using System.Linq;
using SCLauncher.model.definition;

namespace SCLauncher.model.serverinstall;

public class ServerInstallComponent
{

	/// General install order of Sourcemod plugins
	public const int PluginsOrder = 100;

	public required string Title { get; init; }

	public string? Description { get; init; }

	public required int InstallOrder { get; init; }

	/// Ensure that this component is installed
	public bool Required { get; init; }

	/// Other components that must be installed beforehand
	public ImmutableArray<ServerInstallComponent> Dependencies { get; init; } = [];

	/// Hard app compatibility filter, defaults to all apps
	public ImmutableArray<AppType> AppTypes { get; init; } = [.. Enum.GetValues<AppType>()];
	
	/// Soft game-mode preset filter, defaults to all presets
	public ImmutableArray<AppPreset> AppPresets { get; init; } = [.. Enum.GetValues<AppPreset>()];

	public override string ToString()
	{
		return Title;
	}
	
	// -- Singletons --
	
	public static readonly ServerInstallComponent Server = new()
	{
		Title = "Dedicated server",
		InstallOrder = 1,
		Required = true
	};
	
	public static readonly ServerInstallComponent SrcdsFix = new()
	{
		Title = "Fixed server executable",
		Description = "Necessary for running within the launcher",
		InstallOrder = 2,
		Required = true,
		Dependencies = [Server]
	};

	public static readonly ServerInstallComponent MetaMod = new()
	{
		Title = "MetaMod:Source",
		Description = "A modding platform that provides low-level support to other server addons",
		InstallOrder = 3,
		Dependencies = [Server],
		AppTypes = Exclude([AppType.GMOD])
	};
	
	public static readonly ServerInstallComponent SourceMod = new()
	{
		Title = "SourceMod",
		Description = "Administration and scripting framework for Source",
		InstallOrder = 4,
		Dependencies = [MetaMod],
		AppTypes = Exclude([AppType.GMOD])
	};

	public static readonly ServerInstallComponent SourceCoop = new()
	{
		Title = "SourceCoop",
		Description = "Cooperative mod built on SourceMod",
		InstallOrder = PluginsOrder,
		Dependencies = [SourceMod],
		AppTypes = [AppType.BlackMesa, AppType.HL2DM],
		AppPresets = [AppPreset.Cooperative]
	};
	
	public static readonly ServerInstallComponent ModelChooser = new()
	{
		Title = "ModelChooser",
		Description = "Advanced third-person player model chooser",
		InstallOrder = PluginsOrder,
		Dependencies = [SourceMod],
		AppTypes = [AppType.BlackMesa, AppType.HL2DM]
	};

	private static ImmutableArray<TSource> Exclude<TSource>(TSource[] excluded) where TSource : struct, Enum
	{
		return [.. Enum.GetValues<TSource>().Except(excluded)];
	}
	
}