using System;
using System.Collections.Generic;

namespace SCLauncher.model.config;

public partial class ClientConfigurationBlackMesa : ClientConfiguration
{

	public bool Workshop { get; set; } = true;
	
	public bool OldUI { get; set; }
	
	public bool ForceUiD3D9 { get; set; }

	public bool DXVK { get; set; } = OperatingSystem.IsLinux();
	
	public override List<string> ToLaunchParams()
	{
		List<string> list = [];
		
		if (!Workshop)
			list.AddRange("+workshop_disable", "1");

		if (OldUI)
			list.Add("-oldgameui");
		
		if (ForceUiD3D9)
			list.Add("-forceuid3d9");
		
		if (!DXVK)
			list.Add("-disabledxvk");

		list.AddRange(base.ToLaunchParams());
		return list;
	}
	
}