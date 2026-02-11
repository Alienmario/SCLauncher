using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SCLauncher.model.config;

[JsonDerivedType(typeof(ClientConfigurationBlackMesa), typeDiscriminator: "BlackMesa")]
public partial class ClientConfiguration : INotifyPropertyChanged
{
    public enum WindowModeEnum
    {
	    [Description("[Display mode]")]
	    NoChange,
	    [Description("Fullscreen mode")]
        Fullscreen,
	    [Description("Windowed mode")]
        Windowed,
	    [Description("Borderless mode")]
        Borderless
    }

	public bool Steam { get; set; } = true;
	
	public bool Insecure { get; set; }
	
	public bool Dev { get; set; }
	
	public bool Multirun { get; set; }
	
	public WindowModeEnum WindowMode { get; set; } = WindowModeEnum.NoChange;

	public ObservableCollection<CustomParam> CustomParams { get; set; } = [];

	public virtual List<string> ToLaunchParams()
	{
		List<string> list = [];
		
		if (Steam)
			list.Add("-steam");
		
		if (Insecure)
			list.Add("-insecure");
		
		if (Dev)
			list.Add("-dev");
		
		if (Multirun)
			list.Add("-multirun");
		
		switch (WindowMode)
		{
			case WindowModeEnum.Windowed:
				list.Add("-windowed");
				break;
			case WindowModeEnum.Borderless:
				list.Add("-windowed");
				list.Add("-noborder");
				break;
			case WindowModeEnum.Fullscreen:
				list.Add("-fullscreen");
				break;
		}
		
		foreach (CustomParam p in CustomParams)
		{
			list.Add(p.Key);
			if (!string.IsNullOrWhiteSpace(p.Value))
				list.Add(p.Value);
		}
		
		return list;
	}

}