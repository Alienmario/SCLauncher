namespace SCLauncher.model.install;

public class ComponentInfo
{
	public bool Installed { get; init; } = true;
	public bool Installable { get; init; } = true;
	public bool Upgradable { get; init; }

	public string? Version { get; init; }
	public string? UpgradeVersion { get; init; }
	public string? Path { get; init; }

	public static ComponentInfo ReadyToInstall { get; } = new() { Installable = true, Installed = false };
	public static ComponentInfo DoNotInstall { get; } = new() { Installable = false, Installed = false };
}