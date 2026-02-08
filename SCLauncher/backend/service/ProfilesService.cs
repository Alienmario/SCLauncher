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

		if (profiles.Count == 0)
		{
			profiles.Add(AppProfile.CreateDefaultBlackMesa());
			profiles.Add(AppProfile.CreateDefaultHL2DM());
		}

		if (globalConfig.ActiveProfile == null || !SetActiveProfile(globalConfig.ActiveProfile))
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
		AppProfile? profile = profiles.FirstOrDefault(p => p.Name == name);

		// Do not delete the last profile
		if (profile == null || profiles.Count <= 1)
		{
			return false;
		}

		profiles.Remove(profile);

		if (activeProfile == profile)
		{
			SetActiveProfile(profiles[0].Name);
		}

		ProfileDeleted?.Invoke(this, profile);
		return true;
	}

	public bool SetActiveProfile(string name)
	{
		AppProfile? profile = profiles.FirstOrDefault(p => p.Name == name);
		if (profile != null)
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
		if (!SteamUtils.IsValidSteamInstallDir(globalConfig.SteamPath))
			return;

		Task.Run(async () =>
		{
			foreach (AppProfile profile in profilesToUpdate)
			{
				if (!Directory.Exists(profile.GamePath))
				{
					profile.GamePath =
						await SteamUtils.FindAppPathAsync(globalConfig.SteamPath!, profile.GameAppId)
						?? profile.GamePath;
				}

				if (!Directory.Exists(profile.ServerPath))
				{
					profile.ServerPath =
						await SteamUtils.FindAppPathAsync(globalConfig.SteamPath!, profile.ServerAppId)
						?? profile.ServerPath;
				}
			}
		}).Wait();
	}
}
