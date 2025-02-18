using Avalonia.Controls;
using Avalonia.Data;
using SCLauncher.model;

namespace SCLauncher.ui.views;

public partial class Settings : UserControl
{
	
	public Settings()
	{
		InitializeComponent();
		
		var config = App.GetService<ConfigHolder>();
		
		GamePath.Bind(TextBox.TextProperty, new Binding {Source = config, Path = nameof(config.GamePath)});
		ServerPath.Bind(TextBox.TextProperty, new Binding {Source = config, Path = nameof(config.ServerPath)});
	}
	
}