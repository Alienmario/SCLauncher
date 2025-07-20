namespace ModSupportLib.Repository;

public enum ModPathType
{
	/// Fully qualified path
	AbsolutePath,
	/// Relative to current directory
	RelativePath,
	/// Relative to the specific mod's workshop directory
	RelativeWorkshopPath
}