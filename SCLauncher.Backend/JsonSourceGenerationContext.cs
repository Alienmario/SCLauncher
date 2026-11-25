using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using SCLauncher.Shared.Model.Config;

namespace SCLauncher.Backend;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(GlobalConfiguration))]
[JsonSerializable(typeof(ServerConfiguration))]
[JsonSerializable(typeof(ServerConfigurationSDK2013))]
[JsonSerializable(typeof(ServerConfigurationHL2DM))]
[JsonSerializable(typeof(ClientConfiguration))]
[JsonSerializable(typeof(ClientConfigurationBlackMesa))]
[JsonSerializable(typeof(List<AppProfile>))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
	/// <summary>
	/// <see cref="Default"/> with dynamic polymorphic surrogates applied.
	/// Resolves subtypes based on related runtime types.
	/// </summary>
	public static IJsonTypeInfoResolver Resolver => field ??=
		Default.WithAddedModifier(Modifiers.ApplyDynamicSurrogates);
}

file static class Modifiers
{
	public static void ApplyDynamicSurrogates(JsonTypeInfo typeInfo)
	{
		if (typeInfo.Type == typeof(AppProfile))
		{
			ReplaceWithDynamicSurrogate<AppProfile, ServerConfiguration>(
				typeInfo,
				nameof(AppProfile.ServerConfig),
				p => p.ServerConfig,
				(p, val) => p.ServerConfig = val);

			ReplaceWithDynamicSurrogate<AppProfile, ClientConfiguration>(
				typeInfo,
				nameof(AppProfile.ClientConfig),
				p => p.ClientConfig,
				(p, val) => p.ClientConfig = val);
		}
	}

	/// <summary>
	/// Replaces a declared base property with a <see cref="JsonElement"/> surrogate so that it is serialized
	/// using its concrete runtime type without a discriminator, and deserialized directly into that concrete type.
	/// </summary>
	private static void ReplaceWithDynamicSurrogate<TOwner, TProperty>(
		JsonTypeInfo typeInfo,
		string propertyName,
		Func<TOwner, TProperty?> getter,
		Action<TOwner, TProperty> setter)
	{
		var props = typeInfo.Properties;
		var prop = props.FirstOrDefault(p => p.Name == propertyName);
		if (prop == null)
			return;

		int index = props.IndexOf(prop);

		var surrogateInfo = new JsonPropertyInfoValues<JsonElement>
		{
			IsProperty = true,
			IsPublic = true,
			DeclaringType = typeof(TOwner),
			PropertyTypeInfo = typeInfo.Options.GetTypeInfo(typeof(JsonElement)),
			PropertyName = propertyName,
			Getter = obj =>
			{
				if (obj is not TOwner owner || getter(owner) is not { } value)
					return default;

				var concreteTypeInfo = typeInfo.Options.GetTypeInfo(value.GetType());
				return JsonSerializer.SerializeToElement(value, concreteTypeInfo);
			},
			Setter = (obj, element) =>
			{
				if (obj is not TOwner owner || element.ValueKind != JsonValueKind.Object)
					return;

				if (getter(owner) is not { } existingValue)
					return;

				var concreteTypeInfo = typeInfo.Options.GetTypeInfo(existingValue.GetType());
				setter(owner, (TProperty)element.Deserialize(concreteTypeInfo)!);
			}
		};

		// JsonMetadataServices constructs the strongly-typed JsonPropertyInfo<JsonElement> directly, avoiding the
		// reflection-based JsonTypeInfo.CreateJsonPropertyInfo(Type, string).
		var surrogate = JsonMetadataServices.CreatePropertyInfo(typeInfo.Options, surrogateInfo);
		surrogate.ShouldSerialize = (_, value) => value is JsonElement { ValueKind: not JsonValueKind.Undefined };

		props[index] = surrogate;
	}
}