namespace SCLauncher.model.serverinstall;

public class ServerInstallComponent
{
	public const int PluginsOrder = 100;

	public required string Title { get; init; }
	
	public string? Description { get; init; }

	public required int InstallOrder { get; init; }

	public required bool Optional { get; init; }

	public override string ToString()
	{
		return Title;
	}
	
	// -- Singletons --
	
	public static readonly ServerInstallComponent Server = new()
	{
		Title = "Dedicated server",
		InstallOrder = 1,
		Optional = false
	};
	
	public static readonly ServerInstallComponent SrcdsFix = new()
	{
		Title = "Fixed server executable",
		Description = "Necessary for running within the launcher",
		InstallOrder = 2,
		Optional = false
	};

	public static readonly ServerInstallComponent MetaMod = new()
	{
		Title = "MetaMod:Source",
		Description = "A modding platform that provides low-level support to other server addons",
		InstallOrder = 3,
		Optional = false
	};
	
	public static readonly ServerInstallComponent SourceMod = new()
	{
		Title = "SourceMod",
		Description = "Administration and scripting framework for Source",
		InstallOrder = 4,
		Optional = false
	};

	public static readonly ServerInstallComponent SourceCoop = new()
	{
		Title = "SourceCoop",
		Description = "Cooperative mod built on SourceMod",
		InstallOrder = PluginsOrder,
		Optional = true
	};
	
	public static readonly ServerInstallComponent ModelChooser = new()
	{
		Title = "ModelChooser",
		Description = "Advanced third-person player model chooser",
		InstallOrder = PluginsOrder,
		Optional = true
	};
	
}