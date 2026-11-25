using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SCLauncher.Shared.Extensions;
using SCLauncher.Shared.Service;

namespace SCLauncher.Backend.Service;

internal class PersistenceService : IPersistenceService
{
	private readonly string _dir;

	private readonly Dictionary<string, (object Instance, IJsonTypeInfoResolver? Context)> _persistedObjects = new();

	private readonly JsonSerializerOptions _serializerOptions = new();

	public bool Available { get; private set; }

	public string? DataDirectory => Available ? _dir : null;

	public bool PrettyPrint
	{
		get => _serializerOptions.WriteIndented;
		set => _serializerOptions.WriteIndented = value;
	}
	
	public PersistenceService()
	{
		PrettyPrint = true;
		
		try
		{
			_dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		}
		catch (Exception e)
		{
			e.Log();
		}
		
		if (string.IsNullOrWhiteSpace(_dir))
		{
			_dir = ".";
		}
		
		_dir = Path.Join(_dir, "SCLauncher");
		try
		{
			Directory.CreateDirectory(_dir);
			Available = true;
		}
		catch (Exception e)
		{
			e.Log();
		}
		
		AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
	}
	
	private void OnProcessExit(object? sender, EventArgs e)
	{
		SaveAll();
	}

	public void Bind(string name, object instance, IJsonTypeInfoResolver? context = null, bool loadNow = true)
	{
		_persistedObjects[name] = (instance, context);
		if (loadNow)
		{
			LoadAndMerge(name, instance, context);
		}
	}
	
	public bool Forget(string name)
	{
		return _persistedObjects.Remove(name);
	}

	public void SaveAll()
	{
		foreach (var (name, (instance, context)) in _persistedObjects)
		{
			Save(name, instance, context);
		}
	}
	
	public void LoadAll()
	{
		foreach (var (name, (instance, context)) in _persistedObjects)
		{
			LoadAndMerge(name, instance, context);
		}
	}

	public bool Save(string name, object o, IJsonTypeInfoResolver? context = null)
	{
		if (!Available)
			return false;
		
		string path = Path.Join(_dir, name + ".json");
		try
		{
			JsonTypeInfo typeInfo = GetTypeInfo(o.GetType(), context);
			string json = JsonSerializer.Serialize(o, typeInfo);
			File.WriteAllText(path, json);
			return true;
		}
		catch (Exception e)
		{
			e.Log();
			return false;
		}
	}

	public void LoadAndMerge(string name, object instance, IJsonTypeInfoResolver? context = null)
	{
		if (instance is IEnumerable and not IList)
			throw new NotSupportedException($"Cannot merge into collection of type {instance.GetType()}, only {nameof(IList)} is supported");

		object? loaded = Load(name, instance.GetType(), context);
		if (loaded != null)
		{
			if (instance is IList destList)
			{
				if (loaded is IEnumerable srcEnumerable)
				{
					destList.Clear();
					foreach (var item in srcEnumerable)
					{
						destList.Add(item);
					}
				}
			}
			else
			{
				MergeProperties(loaded, instance, context);
			}
		}
	}
	
	public object? Load(string name, Type type, IJsonTypeInfoResolver? context = null)
	{
		if (!Available)
			return null;
		
		string path = Path.Join(_dir, name + ".json");
		try
		{
			string json = File.ReadAllText(path);
			JsonTypeInfo typeInfo = GetTypeInfo(type, context);
			return JsonSerializer.Deserialize(json, typeInfo);
		}
		catch (Exception e)
		{
			if (e is not FileNotFoundException)
			{
				e.Log();
			}
			
			return null;
		}
	}

	private JsonTypeInfo GetTypeInfo(Type type, IJsonTypeInfoResolver? context)
	{
		var options = new JsonSerializerOptions(_serializerOptions)
		{
			TypeInfoResolver = context
		};
		return options.GetTypeInfo(type);
	}

	private void MergeProperties(object source, object target, IJsonTypeInfoResolver? context)
	{
		JsonTypeInfo typeInfo = GetTypeInfo(target.GetType(), context);
		foreach (JsonPropertyInfo prop in typeInfo.Properties)
		{
			if (prop.Get != null && prop.Set != null)
			{
				prop.Set(target, prop.Get(source));
			}
		}
	}

	/// <summary>
	/// Deletes all files and subdirectories within the data persistence directory.
	/// Marks persistence as unavailable.
	/// </summary>
	public void EraseAll()
	{
		if (!Available)
			throw new Exception("Persistence unavailable");
		
		var dirInfo = new DirectoryInfo(_dir);
		foreach (var file in dirInfo.GetFiles())
		{
			file.Delete();
		}
		foreach (var subDir in dirInfo.GetDirectories())
		{
			subDir.Delete(true);
		}
		
		Available = false;
	}
	
}