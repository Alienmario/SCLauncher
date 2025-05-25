using Avalonia.Controls;
using SCLauncher.model;

namespace SCLauncher.ui.views;

public partial class MainWindow : Window
{
	
	public MainWindow()
	{
		InitializeComponent();
		
		var config = App.GetService<GlobalConfiguration>();
		
		foreach (object? item in Tabs.Items)
		{
			if (item is TabItem tabItem && tabItem.Name == config.CurrentTab)
			{
				Tabs.SelectedItem = item;
				break;
			}
		}

		Tabs.SelectionChanged += (sender, args) =>
		{
			if (Tabs.SelectedItem is TabItem tabItem)
			{
				config.CurrentTab = tabItem.Name;
			}
		};
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