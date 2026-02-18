using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SCLauncher.backend.util;
using SCLauncher.model.config;

namespace SCLauncher.backend.service;

public class ProfilesService(GlobalConfiguration globalConfig, PersistenceService persistence)
{
	// fields
	private readonly List<AppProfile> profiles = [];
	private AppProfile? activeProfile;

	// properties
	public IReadOnlyList<AppProfile> Profiles => profiles.AsReadOnly();
	public AppProfile ActiveProfile => activeProfile ?? throw new InvalidOperationException("Active profile not set");

	// events
	public event EventHandler<AppProfile>? ProfileSwitched;
	public event EventHandler<AppProfile>? ProfileAdded;
	public event EventHandler<AppProfile>? ProfileDeleted;

	internal void Initialize()
	{
		persistence.Bind("profiles", profiles, JsonSourceGenerationContext.Default);

		// Try to set active profile
		if (globalConfig.ActiveProfile == null || !SetActiveProfile(globalConfig.ActiveProfile))
		{
			if (profiles.Count > 0)
			{
				SetActiveProfile(profiles[0]);
			}
		}

		UpdateProfilePaths(profiles);
	}

	public AppProfile CreateProfile(AppType appType, string name)
	{
		AppProfile profile = AppProfile.Create(appType);
		profile.Name = name;
		UpdateProfilePaths([profile]);

		profiles.Add(profile);
		ProfileAdded?.Invoke(this, profile);

		// Ensure we have an active profile
		if (profiles.Count == 1)
		{
			SetActiveProfile(profile);
		}
		
		return profile;
	}

	public bool DeleteProfile(string name)
	{
		AppProfile? profile = profiles.FirstOrDefault(p => p.Name == name);

		// Do not delete the last profile
		if (profile == null || profiles.Count <= 1)
		{
			return false;
		}

		profiles.Remove(profile);

		if (activeProfile == profile)
		{
			SetActiveProfile(profiles[0]);
		}

		ProfileDeleted?.Invoke(this, profile);
		return true;
	}

	public bool SetActiveProfile(string name)
	{
		AppProfile? profile = profiles.FirstOrDefault(p => p.Name == name);
		return profile != null && SetActiveProfile(profile);
	}
	
	private bool SetActiveProfile(AppProfile profile)
	{
		if (activeProfile != profile)
		{
			activeProfile?.PropertyChanged -= ActiveProfilePropertyChanged;
			globalConfig.ActiveProfile = profile.Name;
			activeProfile = profile;
			activeProfile.PropertyChanged += ActiveProfilePropertyChanged;
			ProfileSwitched?.Invoke(this, activeProfile);
			return true;
		}
		return false;
	}
	
	private void ActiveProfilePropertyChanged(object? sender, PropertyChangedEventArgs args)
	{
		if (args.PropertyName == nameof(AppProfile.Name))
			globalConfig.ActiveProfile = (sender as AppProfile)!.Name;
	}

	private void UpdateProfilePaths(IEnumerable<AppProfile> profilesToUpdate)
	{
		string? steamPath = globalConfig.SteamPath;
		if (!SteamUtils.IsValidSteamInstallDir(steamPath))
			return;

		Task.Run(async () =>
		{
			foreach (AppProfile profile in profilesToUpdate)
			{
				if (!Directory.Exists(profile.GamePath))
				{
					profile.GamePath = await SteamUtils.FindAppPathAsync(steamPath!, profile.GameAppId)
					                   ?? profile.GamePath;
				}

				if (!Directory.Exists(profile.ServerPath))
				{
					profile.ServerPath = await SteamUtils.FindAppPathAsync(steamPath!, profile.ServerAppId)
					                     ?? profile.ServerPath;
				}
			}
		}).Wait();
	}
}
