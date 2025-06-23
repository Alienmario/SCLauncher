using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SCLauncher.model.config;

public partial class ClientConfiguration : INotifyPropertyChanged
{

	public bool Steam { get; set; } = true;
	
	public bool Insecure { get; set; }
	
	public bool Dev { get; set; }
	
	public bool Multirun { get; set; }

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
		
		foreach (CustomParam p in CustomParams)
		{
			list.Add(p.Key);
			if (!string.IsNullOrWhiteSpace(p.Value))
				list.Add(p.Value);
		}
		
		return list;
	}

}