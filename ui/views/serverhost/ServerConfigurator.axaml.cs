using Avalonia.Controls;
using SCLauncher.backend.service;

namespace SCLauncher.ui.views.serverhost;

public partial class ServerConfigurator : UserControl
{
	
	public ServerConfigurator()
	{
		InitializeComponent();
		
		if (Design.IsDesignMode)
			return;

		DataContext = App.GetService<BackendService>().GetServerConfig();
	}

	public void ResetToDefaults()
	{
		DataContext = App.GetService<BackendService>().GetServerConfig(true);
	}
	
}