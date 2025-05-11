using SCLauncher.model;
using SCLauncher.model.serverinstall;

namespace SCLauncher.ui.design;

public class DServerInstallParams : ServerInstallParams
{
	public DServerInstallParams()
	{
		AppInfo = new DAppInfo();
		Method = ServerInstallMethod.External;
		Path = @"C:\Program Files\Program Files\Program Files\Program Files\Program Files\Program Files";
	}
}

public class DAppInfo() : AppInfo(362890, 346680, "Black Mesa", "Black Mesa Dedicated Server", "bms")
{
}