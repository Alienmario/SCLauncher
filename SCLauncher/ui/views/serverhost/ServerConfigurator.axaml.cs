using Avalonia.Controls;
using SCLauncher.backend.service;
using SCLauncher.model.config;

namespace SCLauncher.ui.views.serverhost;

public partial class ServerConfigurator : UserControl
{
	private readonly BackendService backend;
	
	public ServerConfigurator()
	{
		InitializeComponent();
		backend = App.GetService<BackendService>();
		
		if (Design.IsDesignMode)
			return;

		backend.ProfileSwitched += OnProfileSwitched;
		OnProfileSwitched(this, backend.ActiveProfile);
	}

	private void OnProfileSwitched(object? sender, AppProfile newProfile)
	{
		DataContext = newProfile.ServerConfig;
	}

	public void ResetToDefaults()
	{
		DataContext = backend.ActiveProfile.NewServerConfig();
	}
	
}