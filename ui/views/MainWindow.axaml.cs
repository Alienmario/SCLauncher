using Avalonia.Controls;

namespace SCLauncher.ui.views;

public partial class MainWindow : Window
{
	
	public MainWindow()
	{
		InitializeComponent();
	}

	public void GoToSettings()
	{
		SettingsTab.IsSelected = true;
	}

	public void GoToSingleplayer()
	{
		SingleplayerTab.IsSelected = true;
	}
	
	public void GoToMods()
	{
		ModsTab.IsSelected = true;
	}
	
	public void GoToHostServer()
	{
		HostServerTab.IsSelected = true;
	}
	
}