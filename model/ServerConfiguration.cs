using System.Collections.Generic;
using System.ComponentModel;

namespace SCLauncher.model;

public partial class ServerConfiguration : INotifyPropertyChanged
{
	
	public string? StartMap { get; set; }

	public byte Maxplayers { get; set; } = 32;

	public string? Ip { get; set; } = "0.0.0.0";

	public ushort? Port { get; set; } = 27015;
	
	public bool StrictPortBind { get; set; }
	
	public bool Lan { get; set; }
	
	public bool? Teamplay { get; set; }

	public Dictionary<string, string> ExtraParams { get; set; } = [];

	public List<string> ToLaunchParams()
	{
		List<string> list = [];
		
		// - params first
		
		if (Ip != null)
			list.AddRange(["-ip", Ip]);
		
		if (Port != null)
			list.AddRange(["-port", Port.Value.ToString()]);
		
		if (StrictPortBind)
			list.Add("-strictportbind");
		
		foreach ((string key, string value) in ExtraParams)
		{
			if (!key.StartsWith('+'))
				list.AddRange([key, value]);
		}
		
		// + params now
		
		if (Teamplay != null)
			list.AddRange(["+mp_teamplay", Teamplay.Value ? "1" : "0"]);
		
		if (Lan)
			list.AddRange(["+sv_lan", "1"]);
		
		list.AddRange(["+maxplayers", Maxplayers.ToString()]);

		if (StartMap != null)
			list.AddRange(["+map", StartMap]);
		
		foreach ((string key, string value) in ExtraParams)
		{
			if (key.StartsWith('+'))
				list.AddRange([key, value]);
		}
		
		return list;
	}

}