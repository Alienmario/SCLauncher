using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCLauncher.backend.service;

public class PersistenceService
{
	private readonly string dir;

	private readonly Dictionary<string, (object Instance, JsonSerializerContext? Context)> persistedObjects = new();

	private readonly JsonSerializerOptions serializerOptions = new();

	public bool Available { get; }

	public bool PrettyPrint
	{
		get => serializerOptions.WriteIndented;
		set => serializerOptions.WriteIndented = value;
	}
	
	public PersistenceService()
	{
		PrettyPrint = true;
		
		try
		{
			dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		}
		catch (Exception e)
		{
			e.Log();
		}
		
		if (string.IsNullOrWhiteSpace(dir))
		{
			dir = ".";
		}
		
		dir = Path.Join(dir, "SCLauncher");
		try
		{
			Directory.CreateDirectory(dir);
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

	public void Bind(string name, object instance, JsonSerializerContext? context = null, bool loadNow = true)
	{
	    persistedObjects[name] = (instance, context);
		if (loadNow)
		{
	        LoadAndMerge(name, instance, context);
		}
	}
	
	public bool Forget(string name)
	{
		return persistedObjects.Remove(name);
	}

	public void SaveAll()
	{
		foreach (var (name, (instance, context)) in persistedObjects)
		{
		    Save(name, instance, context);
		}
	}
	
	public void LoadAll()
	{
		foreach (var (name, (instance, context)) in persistedObjects)
		{
		    LoadAndMerge(name, instance, context);
		}
	}

	public bool Save(string name, object o, JsonSerializerContext? context = null)
	{
		if (!Available)
			return false;
		
		string json = JsonSerializer.Serialize(o, new JsonSerializerOptions(serializerOptions)
		{
			TypeInfoResolver = context
		});
		
		string path = Path.Join(dir, name + ".json");
		try
		{
			File.WriteAllText(path, json);
			return true;
		}
		catch (Exception e)
		{
			e.Log();
			return false;
		}
	}

	public void LoadAndMerge(string name, object instance, JsonSerializerContext? context = null)
	{
		object? loaded = Load(name, instance.GetType(), context);
		if (loaded != null)
		{
			MergeProperties(loaded, instance);
		}
	}
	
	public object? Load(string name, Type type, JsonSerializerContext? context = null)
	{
		if (!Available)
			return null;
		
		string path = Path.Join(dir, name + ".json");
		try
		{
			string json = File.ReadAllText(path);
			return JsonSerializer.Deserialize(json, type, new JsonSerializerOptions(serializerOptions)
			{
				TypeInfoResolver = context
			});
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

	private static void MergeProperties(object source, object target)
	{
		foreach (var prop in source.GetType().GetProperties())
		{
			var targetProp = target.GetType().GetProperty(prop.Name);
			if (targetProp != null && targetProp.CanWrite)
			{
				targetProp.SetValue(target, prop.GetValue(source));
			}
		}
	}
	
}