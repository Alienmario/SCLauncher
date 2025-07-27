using System.Text.Json;
using ModSupportLib.Local;
using ModSupportLib.Repository;

namespace ModSupportLib.Service;

public static class ModSupportService
{
	private static readonly HttpClient HttpClient = new();
	
	public static async Task<ModRepository> LoadRepositoryAsync(Uri uri, CancellationToken ct = default)
	{
		try
		{
			string json;
			if (!uri.IsAbsoluteUri)
			{
				json = await File.ReadAllTextAsync(uri.OriginalString, ct);
			}
			else if (uri.IsFile)
			{
				json = await File.ReadAllTextAsync(uri.LocalPath, ct);
			}
			else
			{
				json = await HttpClient.GetStringAsync(uri, ct);
			}

			ModRepository repository = JsonSerializer.Deserialize(json, JsonSourceGenerationContext.Default.ModRepository)
			                              ?? throw new InvalidOperationException("JSON equals to null");
			repository.LoadLocation = uri;
			return repository;
		}
		catch (Exception e)
		{
			return new ModRepository
			{
				LoadLocation = uri,
				LoadException = e
			};
		}
	}

	public static List<ModInfo> QueryMods(List<ModRepository> repositories, ModQuery query)
	{
		List<ModInfo> mods = [];
		foreach (var repository in repositories)
		{
			foreach (var modInfo in repository.Supported)
			{
				if (PassesQueryFilter(modInfo, query))
					mods.Add(modInfo);
			}
		}
		return mods;
	}
	
	public static ModInfo? QueryMod(List<ModRepository> repositories, ModQuery query)
	{
		foreach (var repository in repositories)
		{
			foreach (var modInfo in repository.Supported)
			{
				if (PassesQueryFilter(modInfo, query))
					return modInfo;
			}
		}
		return null;
	}

	public static async Task<ModLocalState?> GetModLocalStateAsync(CommonArgs commonArgs, ModInfo modInfo)
	{
		string absModPath = GetModAbsPath(commonArgs, modInfo);
		
		if (!Directory.Exists(absModPath))
			return null;

		return new ModLocalState
		{
			AbsolutePath = absModPath
		};
	}

	public static async Task<ModLocalState> InstallMod(CommonArgs commonArgs, ModInfo modInfo, CancellationToken ct = default)
	{
		ModLocalState? state = await GetModLocalStateAsync(commonArgs, modInfo);
		if (state == null)
		{
			state = new ModLocalState
			{
				AbsolutePath = ""
			};
		}
		return state;
	}
	
	public static async Task UninstallMod(CommonArgs commonArgs, ModInfo modInfo)
	{
		
	}
	
	public static async Task SetActiveMods(CommonArgs commonArgs, List<ModInfo> mods)
	{
		
	}

	public static string GetModAbsPath(CommonArgs commonArgs, ModInfo modInfo)
	{
		return modInfo.GetPathType() switch
		{
			ModPathType.AbsolutePath => modInfo.Path,
			ModPathType.RelativePath => Path.GetFullPath(modInfo.Path),
			ModPathType.RelativeWorkshopPath => Path.GetFullPath(Path.Join(
				commonArgs.WorkshopPath ?? throw new ArgumentException("Missing workshop path"),
				modInfo.Workshop.ToString(),
				modInfo.Path)),
			
			_ => throw new ArgumentException("Invalid path type")
		};
	}
	
	public static bool PassesQueryFilter(ModInfo info, ModQuery query)
	{
		if (query.Names.Count > 0)
		{
			if (!query.Names.Contains(info.Name) && (info.Short == null || !query.Names.Contains(info.Short)))
				return false;
		}
		if (query.WorkshopIds.Count > 0)
		{
			if (info.Workshop == null || !query.WorkshopIds.Contains((ulong)info.Workshop))
				return false;
		}
		return true;
	}
}