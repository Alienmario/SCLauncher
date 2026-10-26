using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using SCLauncher.model.config;

namespace SCLauncher.backend;

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

		var surrogate = typeInfo.CreateJsonPropertyInfo(typeof(JsonElement), propertyName);

		surrogate.Get = obj =>
		{
			if (obj is not TOwner owner)
				return null;

			var value = getter(owner);
			if (value == null)
				return null;

			var concreteTypeInfo = typeInfo.Options.GetTypeInfo(value.GetType());
			return JsonSerializer.SerializeToElement(value, concreteTypeInfo);
		};

		surrogate.Set = (obj, value) =>
		{
			if (obj is not TOwner owner || value is not JsonElement { ValueKind: JsonValueKind.Object } element)
				return;

			var existingValue = getter(owner);
			if (existingValue == null)
				return;

			var concreteTypeInfo = typeInfo.Options.GetTypeInfo(existingValue.GetType());
			setter(owner, (TProperty)element.Deserialize(concreteTypeInfo)!);
		};

		props[index] = surrogate;
	}
}