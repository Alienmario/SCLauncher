using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCLauncher.backend.service;

public class PersistenceService
{
	private readonly string _dir;

	private readonly Dictionary<string, (object Instance, JsonSerializerContext? Context)> _persistedObjects = new();

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

	public void Bind(string name, object instance, JsonSerializerContext? context = null, bool loadNow = true)
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

	public bool Save(string name, object o, JsonSerializerContext? context = null)
	{
		if (!Available)
			return false;
		
		string json = JsonSerializer.Serialize(o, new JsonSerializerOptions(_serializerOptions)
		{
			TypeInfoResolver = context
		});
		
		string path = Path.Join(_dir, name + ".json");
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
			if (instance is IEnumerable and not string)
			{
				if (loaded is IEnumerable srcEnumerable and not string)
				{
					dynamic dynamicCollection = instance;
					dynamicCollection.Clear();
					foreach (var item in srcEnumerable)
					{
						dynamicCollection.Add((dynamic)item);
					}
				}
			}
			else
			{
				MergeProperties(loaded, instance);
			}
		}
	}
	
	public object? Load(string name, Type type, JsonSerializerContext? context = null)
	{
		if (!Available)
			return null;
		
		string path = Path.Join(_dir, name + ".json");
		try
		{
			string json = File.ReadAllText(path);
			return JsonSerializer.Deserialize(json, type, new JsonSerializerOptions(_serializerOptions)
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