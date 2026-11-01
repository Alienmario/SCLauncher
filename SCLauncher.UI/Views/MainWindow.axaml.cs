using Avalonia.Controls;
using SCLauncher.Shared.Model.Config;

namespace SCLauncher.UI.Views;

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

	public void GoToGame()
	{
		GameTab.IsSelected = true;
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