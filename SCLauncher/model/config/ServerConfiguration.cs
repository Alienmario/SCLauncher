using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SCLauncher.model.config;

[JsonDerivedType(typeof(ServerConfigurationHl2dm), typeDiscriminator: "HL2DM")]
public partial class ServerConfiguration : INotifyPropertyChanged
{

	public string? ServerCfgFile { get; set; } = "server.cfg";

	public string? Hostname { get; set; } = "SCLauncher #" + Random.Shared.Next(1000, 9999);
	
	public string? Password { get; set; }
	
	public string? StartMap { get; set; }

	public byte Maxplayers { get; set; } = 32;

	public string? Ip { get; set; } = "0.0.0.0";

	public ushort? Port { get; set; } = 27015;
	
	public bool StrictPortBind { get; set; }
	
	public bool Lan { get; set; }
	
	public bool? Teamplay { get; set; }

	public ObservableCollection<CustomParam> CustomParams { get; set; } = [];

	public virtual List<string> ToLaunchParams()
	{
		List<string> list = [];
		
		// - params first
		
		if (Ip != null)
			list.AddRange(["-ip", Ip]);
		
		if (Port != null)
			list.AddRange(["-port", Port.Value.ToString()]);
		
		if (StrictPortBind)
			list.Add("-strictportbind");
		
		foreach (CustomParam p in CustomParams)
		{
			if (!p.Key.StartsWith('+'))
			{
				list.Add(p.Key);
				if (!string.IsNullOrWhiteSpace(p.Value))
					list.Add(p.Value);
			}
		}
		
		// + params now
		
		if (!string.IsNullOrEmpty(ServerCfgFile))
			list.AddRange(["+servercfgfile", ServerCfgFile]);
		
		if (!string.IsNullOrEmpty(Hostname))
			list.AddRange(["+hostname", Hostname]);
		
		if (!string.IsNullOrEmpty(Password))
			list.AddRange(["+sv_password", Password]);
		
		if (Lan)
			list.AddRange(["+sv_lan", "1"]);
		
		list.AddRange(["+maxplayers", Maxplayers.ToString()]);

		if (Teamplay != null)
			list.AddRange(["+mp_teamplay", Teamplay.Value ? "1" : "0"]);

		if (!string.IsNullOrEmpty(StartMap))
			list.AddRange(["+map", StartMap]);
		
		foreach (CustomParam p in CustomParams)
		{
			if (p.Key.StartsWith('+'))
				list.AddRange([p.Key, p.Value]);
		}
		
		return list;
	}

}