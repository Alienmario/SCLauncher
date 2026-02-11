using System.ComponentModel;

namespace SCLauncher.model.config;

public partial class GlobalConfiguration : INotifyPropertyChanged
{
	
	public string? SteamPath { get; set; }
	
	public string? SteamWebApiKey { get; set; }
	
	public string? CurrentTab { get; set; }

	public string? ActiveProfile { get; set; }
	
	public bool CheckForUpdates { get; set; } = true;

}