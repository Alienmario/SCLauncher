using System.Text.Json.Serialization;
using SCLauncher.model;

namespace SCLauncher.backend;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(GlobalConfiguration))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
	
}