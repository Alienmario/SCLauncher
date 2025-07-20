using System.Text.Json.Serialization;

namespace ModSupportLib.Repository;

public class ModRepository
{
	public List<ModInfo> Supported { get; set; } = [];

	[JsonIgnore] public RepositoryType Type { get; internal set; } = RepositoryType.Unknown;
	[JsonIgnore] public Uri LoadLocation { get; internal set; } = null!;
	[JsonIgnore] public Exception? LoadException { get; internal set; }
}