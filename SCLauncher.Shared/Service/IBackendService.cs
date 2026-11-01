using System;

namespace SCLauncher.Shared.Service;

public interface IBackendService
{
	event EventHandler<bool>? CanSwitchProfilesChanged;

	bool CanSwitchProfiles { get; }

	void Initialize();
}
