namespace SCLauncher.model.serverinstall;

public enum ServerAvailability
{
	/// Not installed
	Unavailable,
	/// Requires additional components to run
	PartiallyInstalled,
	/// Available for launching
	Available
}