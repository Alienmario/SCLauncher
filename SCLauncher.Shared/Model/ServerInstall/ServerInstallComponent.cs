using System;
using System.Collections.Immutable;
using System.Linq;
using SCLauncher.Shared.Model.Definition;

namespace SCLauncher.Shared.Model.ServerInstall;

public class ServerInstallComponent
{

	/// General install order of Sourcemod plugins
	public const int PluginsOrder = 100;

	public required string Title { get; init; }

	public string? Description { get; init; }

	public string? Url { get; init; }

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
		Description = "Patched SRCDS executable required to run the server inside SCLauncher",
		InstallOrder = 2,
		Required = true,
		Dependencies = [Server]
	};

	public static readonly ServerInstallComponent MetaMod = new()
	{
		Title = "MetaMod:Source",
		Description = "Ground-level modding environment that provides hooking support to dependent addons\n\n" +
		              "Inert on its own, does not alter gameplay in any way.",
		Url = "https://www.sourcemm.net/",
		InstallOrder = 3,
		Dependencies = [Server],
		AppTypes = Exclude([AppType.GMOD])
	};
	
	public static readonly ServerInstallComponent SourceMod = new()
	{
		Title = "SourceMod",
		Description = "Server modding framework built on MetaMod:Source\n\n" +
		              "Provides basic server administration and commands in addition to extension / plugin management.",
		Url = "https://www.sourcemod.net/",
		InstallOrder = 4,
		Dependencies = [MetaMod],
		AppTypes = Exclude([AppType.GMOD])
	};

	public static readonly ServerInstallComponent SourceCoop = new()
	{
		Title = "SourceCoop",
		Description = "Cooperative mod made with SourceMod\n\n" +
		              "Covers all the necessities for a smooth playthrough of supported campaigns and maps in multiplayer.",
		Url = "https://github.com/ampreeT/SourceCoop",
		InstallOrder = PluginsOrder,
		Dependencies = [SourceMod],
		AppTypes = [AppType.BlackMesa, AppType.HL2DM],
		AppPresets = [AppPreset.Cooperative]
	};
	
	public static readonly ServerInstallComponent ModelChooser = new()
	{
		Title = "ModelChooser",
		Description = "SourceMod plugin that lets players customize their player model in game (by typing /model)\n\n" +
		              "Features an interactive third-person preview and extensive customization. Includes default presets." +
		              "\n\nOverrides the game's built-in model selection!",
		Url = "https://github.com/Alienmario/ModelChooser",
		InstallOrder = PluginsOrder,
		Dependencies = [SourceMod],
		AppTypes = [AppType.BlackMesa, AppType.HL2DM]
	};

	private static ImmutableArray<TSource> Exclude<TSource>(TSource[] excluded) where TSource : struct, Enum
	{
		return [.. Enum.GetValues<TSource>().Except(excluded)];
	}
	
}