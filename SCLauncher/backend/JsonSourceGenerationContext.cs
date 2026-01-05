using System.Collections.Generic;
using System.Text.Json.Serialization;
using SCLauncher.model.config;

namespace SCLauncher.backend;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(GlobalConfiguration))]
[JsonSerializable(typeof(ServerConfiguration))]
[JsonSerializable(typeof(ClientConfiguration))]
[JsonSerializable(typeof(ClientConfigurationBlackMesa))]
[JsonSerializable(typeof(List<AppProfile>))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
	
}