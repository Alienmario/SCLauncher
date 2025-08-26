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
		ModRepository repository;
		DateTime? lastChanged = null;
		string? localPath = null;
		
		try
		{
			if (!uri.IsAbsoluteUri)
			{
				localPath = uri.OriginalString;
			}
			else if (uri.IsFile)
			{
				localPath = uri.LocalPath;
			}

			string json;
			if (localPath != null)
			{
				json = await File.ReadAllTextAsync(localPath, ct);
				lastChanged = File.GetLastWriteTime(localPath);
			}
			else
			{
				json = await HttpClient.GetStringAsync(uri, ct);
			}
			
			repository = JsonSerializer.Deserialize(json, JsonSourceGenerationContext.Default.ModRepository)
			             ?? throw new InvalidOperationException("JSON equals to null");
		}
		catch (Exception e)
		{
			repository = new ModRepository { LoadException = e };
		}
		repository.Location = uri;
		repository.LocalPath = localPath;
		repository.LoadTime = DateTime.Now;
		repository.LastChanged = lastChanged;
		return repository;
	}

	public static async Task<ModRepository> RefreshRepositoryAsync(ModRepository repository, CancellationToken ct = default)
	{
		try
		{
			if (repository is { LastChanged: not null, LocalPath: not null })
			{
				if (File.GetLastWriteTime(repository.LocalPath) <= repository.LastChanged)
					return repository;
			}
		}
		catch (Exception) {}
		
		return await LoadRepositoryAsync(repository.Location, ct);
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

	/// <param name="installed">To search in installed mods. By default, searches in supported mods.</param>
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

	/// Installs a mod to the install repository and potentially downloads it to WorkshopPath.
	/// <exception cref="InstallException">for any install issues</exception>
	/// <exception cref="ArgumentException">if required arguments are invalid</exception>
	/// <exception cref="RepositoryLoadException">on install repository load failure</exception>
	public static async Task<ModLocalState> InstallModAsync(CommonArgs commonArgs, ModInfo modInfo,
		DataReceivedEventHandler? messageHandler, bool updateIfInstalled = false, CancellationToken ct = default)
	{
		// check for duplicates
		
		ModRepository installRepo = await LoadRepositoryAsync(commonArgs.InstallRepository, ct);
		installRepo.ThrowIfLoadFailure();
		
		ModInfo? cachedModInfo = QueryMod([installRepo], ModQuery.ForName(modInfo.Name), true);
		if (cachedModInfo != null && !cachedModInfo.Equals(modInfo))
		{
			throw new AlreadyInstalledException(
				"A mod using this name is already installed with different configuration." +
				$" If you want to install it, please uninstall '{cachedModInfo.Name}' first.");
		}
		
		ct.ThrowIfCancellationRequested();
		
		if (modInfo.Workshop != null)
		{
			if (string.IsNullOrWhiteSpace(commonArgs.WorkshopPath))
			{
				throw new ArgumentException("Workshop path is not provided");
			}
		
			// check if already installed
		
			cachedModInfo = QueryMod([installRepo], ModQuery.ForWorkshopId(modInfo.Workshop.Value), true);
			if (cachedModInfo != null)
			{
				if (!cachedModInfo.Equals(modInfo))
				{
					throw new AlreadyInstalledException(
						"A mod using this Workshop ID is already installed with different configuration." +
						$" If you want to install it, please uninstall '{cachedModInfo.Name}' first.");
				}
				if (!updateIfInstalled)
				{
					// verify that mod exists physically and exit early if updates are not allowed
					ModLocalState? state = await GetModLocalStateAsync(commonArgs, cachedModInfo);
					if (state != null)
						return state;
				}
			}
		
			// download via DepotDownloader to the supplied WorkshopPath
			await DownloadWorkshopModAsync(commonArgs, modInfo.Workshop.Value, messageHandler, ct);
		}
		
		// validate installation

		ModLocalState localState = await GetModLocalStateAsync(commonArgs, modInfo)
		    ?? throw new InstallException($"Failed to validate mod installation at '{GetModAbsPath(commonArgs, modInfo)}'");

		// if new installation, append the mod

		if (cachedModInfo == null)
		{
			installRepo = await RefreshRepositoryAsync(installRepo);
			installRepo.ThrowIfLoadFailure();

			try
			{
				installRepo.Installed.Add(modInfo);
				await SaveRepositoryAsync(installRepo);
			}
			catch (Exception e)
			{
				throw new InstallException("Unable to update install repository", e);
			}
		}

		return localState;
	}

	/// Updates a mod if it had been installed via Workshop.
	/// <exception cref="InstallException">for any update issues</exception>
	/// <exception cref="ArgumentException">if required arguments are invalid</exception>
	/// <exception cref="RepositoryLoadException">on install repository load failure</exception>
	public static async Task<bool> UpdateModAsync(CommonArgs commonArgs, ModQuery query,
		DataReceivedEventHandler? messageHandler, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(commonArgs.WorkshopPath))
		{
			throw new ArgumentException("Workshop path is not provided");
		}

		ModRepository installRepo = await LoadRepositoryAsync(commonArgs.InstallRepository, ct);
		installRepo.ThrowIfLoadFailure();
		
		ModInfo? cachedModInfo = QueryMod([installRepo], query, true);
		if (cachedModInfo != null)
		{
			if (cachedModInfo.Workshop == null)
			{
				throw new InstallException("The specified mod does not have a Workshop ID");
			}
			await DownloadWorkshopModAsync(commonArgs, cachedModInfo.Workshop.Value, messageHandler, ct);
			return true;
		}
		return false;
	}

	/// Uninstalls an installed mod.
	/// <exception cref="UninstallException">for any uninstall issues</exception>
	/// <exception cref="ArgumentException">if required arguments are invalid</exception>
	/// <exception cref="RepositoryLoadException">on install repository load failure</exception>
	public static async Task<bool> UninstallModAsync(CommonArgs commonArgs, ModQuery query)
	{
		ModRepository installRepo = await LoadRepositoryAsync(commonArgs.InstallRepository);
		installRepo.ThrowIfLoadFailure();
		
		ModInfo? cachedModInfo = QueryMod([installRepo], query, true);
		if (cachedModInfo != null)
		{
			if (cachedModInfo.Workshop != null)
			{
				if (string.IsNullOrWhiteSpace(commonArgs.WorkshopPath))
				{
					throw new ArgumentException("Workshop path is not provided");
				}
				string wsModPath = Path.Join(commonArgs.WorkshopPath, cachedModInfo.Workshop.ToString());
				if (Directory.Exists(wsModPath))
				{
					try
					{
						Directory.Delete(wsModPath, true);
					}
					catch (Exception e)
					{
						throw new UninstallException("Unable to delete mods's Workshop directory", e);
					}
				}
			}

			try
			{
				installRepo.Installed.Remove(cachedModInfo);
				await SaveRepositoryAsync(installRepo);
			}
			catch (Exception e)
			{
				throw new UninstallException("Unable to update install repository", e);
			}
			return true;
		}
		return false;
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

	private static async Task DownloadWorkshopModAsync(
		CommonArgs commonArgs,
		ulong workshopId, 
		DataReceivedEventHandler? messageHandler,
		CancellationToken ct = default)
	{
		var installPath = Path.Join(commonArgs.WorkshopPath, workshopId.ToString());
		var dlConfig = new PubFileDownloadConfig
		{
			AppId = commonArgs.AppId,
			PublishedFileId = workshopId,
			InstallDirectory = installPath
		};
		
		ct.ThrowIfCancellationRequested();
		
		int dlStatus = await SubProcess.PubFileDownload(dlConfig, messageHandler, messageHandler, null, ct);
		if (dlStatus != SubProcess.Success)
		{
			throw new InstallException("Failed to download mod content from Steam Workshop");
		}
	}
}