using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SCLauncher.backend.util;
using SCLauncher.model.config;

namespace SCLauncher.backend.service;

public class BackendService
{
	// dependencies
	private readonly PersistenceService persistence;
	
	// fields
	private readonly List<AppProfile> profiles = [];

	// properties
	public IReadOnlyList<AppProfile> Profiles => profiles.AsReadOnly();
	public AppProfile ActiveProfile { get; private set; } = null!;
	public GlobalConfiguration GlobalConfig { get; }

	// events
	public event EventHandler<AppProfile>? ProfileSwitched;
	public event EventHandler<AppProfile>? ProfileAdded;
	public event EventHandler<AppProfile>? ProfileDeleted;

	public BackendService(GlobalConfiguration globalConfig, PersistenceService persistence)
	{
		this.GlobalConfig = globalConfig;
		this.persistence = persistence;
		Initialize();
	}

	private void Initialize()
	{
		persistence.Bind("global", GlobalConfig, JsonSourceGenerationContext.Default);
		
		string? steamDir = SteamUtils.FindSteamInstallDir();
		if (steamDir != null)
		{
			GlobalConfig.SteamPath = steamDir;
		}
		
		InitializeProfiles();
	}

	private void InitializeProfiles()
	{
		persistence.Bind("profiles", profiles, JsonSourceGenerationContext.Default);

		if (profiles.Count == 0)
		{
			profiles.Add(AppProfile.CreateDefaultBlackMesa());
			profiles.Add(AppProfile.CreateDefaultHL2DM());
		}

		if (GlobalConfig.ActiveProfile == null || !SetActiveProfile(GlobalConfig.ActiveProfile))
		{
			SetActiveProfile(profiles[0].Name);
		}
		
		UpdateProfilePaths(profiles);
	}
	
	public AppProfile CreateProfile(AppType appType, string name)
	{
		AppProfile profile = appType switch
		{
			AppType.BlackMesa => AppProfile.CreateDefaultBlackMesa(),
			AppType.HL2DM => AppProfile.CreateDefaultHL2DM(),
			_ => throw new ArgumentOutOfRangeException(nameof(appType), appType, null)
		};
		
		profile.Name = name;
		UpdateProfilePaths([profile]);
		
		profiles.Add(profile);
		ProfileAdded?.Invoke(this, profile);
		return profile;
	}

	public bool DeleteProfile(string name)
	{
		var profile = profiles.FirstOrDefault(p => p.Name == name);
		
		// Do not delete the last profile
		if (profile == null || profiles.Count <= 1)
		{
			return false;
		}

		profiles.Remove(profile);

		if (ActiveProfile == profile)
		{
			SetActiveProfile(profiles[0].Name);
		}

		ProfileDeleted?.Invoke(this, profile);
		return true;
	}
	
	public bool SetActiveProfile(string name)
	{
		var profile = profiles.FirstOrDefault(p => p.Name == name);
		if (profile != null)
		{
			GlobalConfig.ActiveProfile = profile.Name;
			if (ActiveProfile != profile)
			{
				ActiveProfile = profile;
				ProfileSwitched?.Invoke(this, ActiveProfile);
				return true;
			}
		}
		return false;
	}

	private void UpdateProfilePaths(IEnumerable<AppProfile> profilesToUpdate)
	{
		if (!SteamUtils.IsValidSteamInstallDir(GlobalConfig.SteamPath))
			return;
		
		Task.Run(async () =>
		{
			foreach (AppProfile profile in profilesToUpdate)
			{
				if (!Directory.Exists(profile.GamePath))
				{
					profile.GamePath =
						await SteamUtils.FindAppPathAsync(GlobalConfig.SteamPath!, profile.GameAppId)
						?? profile.GamePath;
				}

				if (!Directory.Exists(profile.ServerPath))
				{
					profile.ServerPath =
						await SteamUtils.FindAppPathAsync(GlobalConfig.SteamPath!, profile.ServerAppId)
						?? profile.ServerPath;
				}
			}
		}).Wait();
	}
	
}