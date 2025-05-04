using System.ComponentModel;

namespace SCLauncher.model;

public partial class ConfigHolder : INotifyPropertyChanged
{
	
	public string? GamePath { get; set; }
	
	public string? ServerPath { get; set; }
	
	public string? SteamPath { get; set; }
	
	public string? CurrentTab { get; set; }

}