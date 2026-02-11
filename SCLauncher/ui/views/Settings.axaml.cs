using Avalonia.Controls;
using SCLauncher.backend.service;
using SCLauncher.model.config;

namespace SCLauncher.ui.views;

public partial class Settings : UserControl
{
	
	public Settings()
	{
		InitializeComponent();

		var profilesService = App.GetService<ProfilesService>();
		GlobalConfigContent.DataContext = App.GetService<GlobalConfiguration>();
		
		profilesService.ProfileSwitched += OnProfileSwitched;
		OnProfileSwitched(this, profilesService.ActiveProfile);
	}

	private void OnProfileSwitched(object? sender, AppProfile newProfile)
	{
		DataContext = newProfile;
	}

}