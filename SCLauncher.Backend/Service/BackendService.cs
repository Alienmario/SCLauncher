using System;
using SCLauncher.Shared.Model.Config;
using SCLauncher.Shared.Service;
using SCLauncher.Shared.Util;

namespace SCLauncher.Backend.Service;

internal class BackendService(
	GlobalConfiguration globalConfig, IPersistenceService persistence,
	IServerControlService serverControlService, ProfilesService profilesService) : IBackendService
{
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

	public void Initialize()
	{
		serverControlService.StateChanged += (sender, isRunning) => ComputeCanSwitchProfiles();

		persistence.Bind("global", globalConfig, JsonSourceGenerationContext.Resolver);
		
		string? steamPath = SteamUtils.FindSteamInstallPath();
		if (steamPath != null)
		{
			globalConfig.SteamPath = steamPath;
		}
		
		profilesService.Initialize();
	}
	
	private void ComputeCanSwitchProfiles()
	{
		CanSwitchProfiles = !serverControlService.IsRunning;
	}

}