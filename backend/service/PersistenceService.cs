using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SCLauncher.backend.service;

public class PersistenceService
{
	private readonly string _dir;

	private readonly Dictionary<string, object> _persistedObjects = new();

	private readonly JsonSerializerOptions _serializerOptions = new();

	public bool Available { get; }

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
			Debug.WriteLine(e);
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
			Debug.WriteLine(e);
		}
		
		AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
	}
	
	private void OnProcessExit(object? sender, EventArgs e)
	{
		SaveAll();
	}

	public void Bind(string name, object o, bool loadNow = true)
	{
		_persistedObjects.Add(name, o);
		if (loadNow)
		{
			LoadAndMerge(name, o);
		}
	}
	
	public bool Forget(string name)
	{
		return _persistedObjects.Remove(name);
	}

	public void SaveAll()
	{
		foreach (var entry in _persistedObjects)
		{
			Save(entry.Key, entry.Value);
		}
	}
	
	public void LoadAll()
	{
		foreach (var entry in _persistedObjects)
		{
			LoadAndMerge(entry.Key, entry.Value);
		}
	}

	public bool Save(string name, object o)
	{
		if (!Available)
			return false;
		
		string json = JsonSerializer.Serialize(o, _serializerOptions);
		string path = Path.Join(_dir, name + ".json");
		try
		{
			File.WriteAllText(path, json);
			return true;
		}
		catch (Exception e)
		{
			Debug.WriteLine(e);
			return false;
		}
	}

	public void LoadAndMerge(string name, object o)
	{
		object? loaded = Load(name, o.GetType());
		if (loaded != null)
		{
			MergeProperties(loaded, o);
		}
	}
	
	public object? Load(string name, Type type)
	{
		if (!Available)
			return null;
		
		string path = Path.Join(_dir, name + ".json");
		try
		{
			string json = File.ReadAllText(path);
			return JsonSerializer.Deserialize(json, type, _serializerOptions);
		}
		catch (Exception e)
		{
			Debug.WriteLine(e);
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