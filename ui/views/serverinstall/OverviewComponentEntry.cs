using SCLauncher.model.serverinstall;

namespace SCLauncher.ui.views.serverinstall;

public class OverviewComponentEntry
{
	public required ServerInstallComponent Component { get; set; }
	public required string Status { get; set; }
}