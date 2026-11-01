using System;
using System.Text.Json.Serialization.Metadata;

namespace SCLauncher.Shared.Service;

public interface IPersistenceService
{
	bool Available { get; }

	string? DataDirectory { get; }

	bool PrettyPrint { get; set; }

	void Bind(string name, object instance, IJsonTypeInfoResolver? context = null, bool loadNow = true);

	bool Forget(string name);

	void SaveAll();

	void LoadAll();

	bool Save(string name, object o, IJsonTypeInfoResolver? context = null);

	void LoadAndMerge(string name, object instance, IJsonTypeInfoResolver? context = null);

	object? Load(string name, Type type, IJsonTypeInfoResolver? context = null);

	/// <summary>
	/// Deletes all files and subdirectories within the data persistence directory.
	/// Marks persistence as unavailable.
	/// </summary>
	void EraseAll();
}
