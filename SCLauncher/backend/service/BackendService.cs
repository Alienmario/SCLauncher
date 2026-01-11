using System;
using SCLauncher.backend.util;
using SCLauncher.model.config;

namespace SCLauncher.backend.service;

public class BackendService
{
	// dependencies
	private readonly ProfilesService profilesService;
	private readonly ServerControlService serverControlService;
	private readonly GlobalConfiguration globalConfig;
	
	// events
	public event EventHandler<bool>? CanSwitchProfilesChanged;
	
	// properties
	public bool CanSwitchProfiles
	{
		get;
		private set
		{
			if (field != value)
			{
				field = value;
				CanSwitchProfilesChanged?.Invoke(this, value);
			}
		}
	} = true;

	public BackendService(GlobalConfiguration globalConfig, PersistenceService persistence,
		ServerControlService serverControlService, ProfilesService profilesService)
	{
		this.globalConfig = globalConfig;
		this.serverControlService = serverControlService;
		this.profilesService = profilesService;

		serverControlService.StateChanged += (sender, isRunning) => ComputeCanSwitchProfiles();

		persistence.Bind("global", globalConfig, JsonSourceGenerationContext.Default);
		
		string? steamDir = SteamUtils.FindSteamInstallDir();
		if (steamDir != null)
		{
			globalConfig.SteamPath = steamDir;
		}
		
		profilesService.Initialize();
	}
	
	private void ComputeCanSwitchProfiles()
	{
		CanSwitchProfiles = !serverControlService.IsRunning;
	}

}