using System.Text.Json.Serialization;

namespace ModSupportLib.Repository;

public class ModRepository
{
	public List<ModInfo> Supported { get; set; } = [];
	public List<ModInfo> Installed { get; set; } = [];

	[JsonIgnore] public Uri Location { get; internal set; } = null!;
	[JsonIgnore] public Exception? LoadException { get; internal set; }
}