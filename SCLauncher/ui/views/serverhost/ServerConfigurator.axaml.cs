using Avalonia.Controls;
using SCLauncher.backend.service;
using SCLauncher.model.config;

namespace SCLauncher.ui.views.serverhost;

public partial class ServerConfigurator : UserControl
{
	private readonly ProfilesService profilesService;
	
	public ServerConfigurator()
	{
		InitializeComponent();
		profilesService = App.GetService<ProfilesService>();
		
		if (Design.IsDesignMode)
			return;

		profilesService.ProfileSwitched += OnProfileSwitched;
		OnProfileSwitched(this, profilesService.ActiveProfile);
	}

	private void OnProfileSwitched(object? sender, AppProfile newProfile)
	{
		DataContext = newProfile.ServerConfig;
	}

	public void ResetToDefaults()
	{
		DataContext = profilesService.ActiveProfile.NewServerConfig();
	}
	
}