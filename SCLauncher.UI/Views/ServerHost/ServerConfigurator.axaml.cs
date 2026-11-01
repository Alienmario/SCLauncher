using Avalonia.Controls;
using SCLauncher.Shared.Model.Config;
using SCLauncher.Shared.Service;

namespace SCLauncher.UI.Views.ServerHost;

public partial class ServerConfigurator : UserControl
{
	private readonly IProfilesService profilesService;
	
	public ServerConfigurator()
	{
		InitializeComponent();
		profilesService = App.GetService<IProfilesService>();
		
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
		DataContext = profilesService.ActiveProfile.ResetServerConfig();
	}
	
}