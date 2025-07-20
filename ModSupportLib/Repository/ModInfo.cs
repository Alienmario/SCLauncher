namespace ModSupportLib.Repository;

public class ModInfo
{
	public required string Name { get; set; }
	public string? Short { get; set; }
	public string? FirstMap { get; set; }
	public ulong? Workshop { get; set; }
	public required ModType Type { get; set; }
	public string Path { get; set; } = ".";

	public ModPathType GetPathType()
	{
		if (System.IO.Path.IsPathFullyQualified(Path))
		{
			return ModPathType.AbsolutePath;
		}
		return Workshop != null ? ModPathType.RelativeWorkshopPath : ModPathType.RelativePath;
	}
}