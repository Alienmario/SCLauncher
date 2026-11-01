using System;
using System.Collections.Generic;
using SCLauncher.Shared.Model.Config;
using SCLauncher.Shared.Model.Definition;

namespace SCLauncher.Shared.Service;

public interface IProfilesService
{
	IReadOnlyList<AppProfile> Profiles { get; }

	AppProfile ActiveProfile { get; }

	event EventHandler<AppProfile>? ProfileSwitched;

	event EventHandler<AppProfile>? ProfileAdded;

	event EventHandler<AppProfile>? ProfileDeleted;

	AppProfile CreateProfile(AppType type, AppPreset preset, string name);

	bool DeleteProfile(string name);

	bool SetActiveProfile(string name);
}
