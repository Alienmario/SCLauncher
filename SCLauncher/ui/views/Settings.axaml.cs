using Avalonia.Controls;
using SCLauncher.backend.service;
using SCLauncher.model.config;

namespace SCLauncher.ui.views;

public partial class Settings : UserControl
{
	
	public Settings()
	{
		InitializeComponent();

		var backend = App.GetService<BackendService>();
		
		GlobalConfigConent.DataContext = backend.GlobalConfig;
		
		backend.ProfileSwitched += OnProfileSwitched;
		OnProfileSwitched(this, backend.ActiveProfile);
	}

	private void OnProfileSwitched(object? sender, AppProfile newProfile)
	{
		DataContext = newProfile;
	}

}