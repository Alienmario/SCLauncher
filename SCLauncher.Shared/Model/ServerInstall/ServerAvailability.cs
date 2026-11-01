namespace SCLauncher.Shared.Model.ServerInstall;

public enum ServerAvailability
{
	/// Not installed
	Unavailable,
	/// Requires additional components to run
	PartiallyInstalled,
	/// Available for launching
	Available
}