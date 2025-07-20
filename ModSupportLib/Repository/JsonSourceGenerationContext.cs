using System.Text.Json.Serialization;

namespace ModSupportLib.Repository;

[JsonSourceGenerationOptions(
	UseStringEnumConverter = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	PropertyNameCaseInsensitive = true,
	AllowTrailingCommas = true,
	WriteIndented = true)]
[JsonSerializable(typeof(ModRepository))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
}