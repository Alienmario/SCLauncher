using System.Diagnostics;
using System.Text.Json;
using DepotDownloader;
using ModSupportLib.Exceptions;
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
			repository.Location = uri;
			return repository;
		}
		catch (Exception e)
		{
			return new ModRepository
			{
				Location = uri,
				LoadException = e
			};
		}
	}

	internal static async Task SaveRepositoryAsync(ModRepository repository)
	{
		string content = JsonSerializer.Serialize(repository, JsonSourceGenerationContext.Default.ModRepository);
		string? path;
		if (!repository.Location.IsAbsoluteUri)
		{
			path = repository.Location.OriginalString;
		}
		else if (repository.Location.IsFile)
		{
			path = repository.Location.LocalPath;
		}
		else
		{
			throw new InvalidOperationException("Cannot save repository to a non-file URI: " + repository.Location);
		}
		await File.WriteAllTextAsync(path, content);
	}

	/// <param name="installed">To search in installed mods. By default searches in supported mods.</param>
	public static List<ModInfo> QueryMods(List<ModRepository> repositories, ModQuery query, bool installed = false)
	{
		List<ModInfo> mods = [];
		foreach (var repository in repositories)
		{
			List<ModInfo> modList = installed ? repository.Installed : repository.Supported;
			foreach (var modInfo in modList)
			{
				if (PassesQueryFilter(modInfo, query))
					mods.Add(modInfo);
			}
		}
		return mods;
	}
	
	/// <param name="installed">To search in installed mods. By default, searches in supported mods.</param>
	public static ModInfo? QueryMod(List<ModRepository> repositories, ModQuery query, bool installed = false)
	{
		foreach (var repository in repositories)
		{
			List<ModInfo> modList = installed ? repository.Installed : repository.Supported;
			foreach (var modInfo in modList)
			{
				if (PassesQueryFilter(modInfo, query))
					return modInfo;
			}
		}
		return null;
	}

	/// <exception cref="InstallException">for any issues</exception>
	public static async Task<ModLocalState> InstallMod(CommonArgs commonArgs, ModInfo modInfo,
		DataReceivedEventHandler? messageHandler, CancellationToken ct = default)
	{
		if (modInfo.Workshop == null)
		{
			throw new InstallException("Mod is missing Workshop ID");
		}
		if (commonArgs.WorkshopPath == null)
		{
			throw new InstallException("Workshop path is not provided");
		}
		
		// check if already installed
		
		ModRepository installRepo = await LoadRepositoryAsync(commonArgs.InstallRepository, ct);
		ModInfo? cachedModInfo = QueryMod([installRepo], ModQuery.ForWorkshopId(modInfo.Workshop.Value), true);
		if (cachedModInfo != null)
		{
			// todo? check if cached and supplied modinfos differ and force uninstall first
			
			// it's cached in the install repository, verify if exists physically
			ModLocalState? state = await GetModLocalStateAsync(commonArgs, cachedModInfo);
			if (state != null)
				return state;
		}
		
		// download via DepotDownloader to the supplied WorkshopPath
		
		var installPath = Path.Join(commonArgs.WorkshopPath, modInfo.Workshop.ToString());
		var dlConfig = new PubFileDownloadConfig
		{
			AppId = commonArgs.AppId,
			PublishedFileId = modInfo.Workshop.Value,
			InstallDirectory = installPath
		};
		
		int dlStatus = await SubProcess
			.PubFileDownload(dlConfig, messageHandler, messageHandler, null, ct);
		if (dlStatus != SubProcess.Success)
		{
			throw new InstallException("Failed to download mod content from Steam Workshop");
		}

		// reload the install repository and append the mod
		
		installRepo = await LoadRepositoryAsync(commonArgs.InstallRepository);
		if (installRepo.LoadException != null && installRepo.LoadException is not FileNotFoundException)
		{
			throw new InstallException("Failed to load install repository for update", installRepo.LoadException);
		}
		
		installRepo.Installed.Add(modInfo);
		await SaveRepositoryAsync(installRepo);
		
		return await GetModLocalStateAsync(commonArgs, modInfo) ?? throw new InstallException("Failed to verify mod installation");
	}
	
	public static async Task UninstallMod(CommonArgs commonArgs, ModInfo modInfo)
	{
		
	}
	
	public static async Task SetActiveMods(CommonArgs commonArgs, List<ModInfo> mods)
	{
		
	}

	public static async Task<ModLocalState?> GetModLocalStateAsync(CommonArgs commonArgs, ModInfo modInfo)
	{
		string absPath = GetModAbsPath(commonArgs, modInfo);
		
		if (!Directory.Exists(absPath))
			return null;

		return new ModLocalState
		{
			AbsoluteInstallPath = absPath
		};
	}

	public static string GetModAbsPath(CommonArgs commonArgs, ModInfo modInfo)
	{
		return modInfo.GetPathType() switch
		{
			ModPathType.AbsolutePath => modInfo.Path,
			ModPathType.RelativePath => Path.GetFullPath(modInfo.Path),
			ModPathType.RelativeWorkshopPath => Path.GetFullPath(Path.Join(
				commonArgs.WorkshopPath ?? throw new ArgumentException("Workshop path is not provided"),
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