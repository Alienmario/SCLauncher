using System.Text.Json.Serialization;
using SCLauncher.model.config;

namespace SCLauncher.backend;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(GlobalConfiguration))]
[JsonSerializable(typeof(ServerConfiguration))]
[JsonSerializable(typeof(ClientConfiguration))]
[JsonSerializable(typeof(ClientConfigurationBlackMesa))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
	
}