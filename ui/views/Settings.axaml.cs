using Avalonia.Controls;
using SCLauncher.model;

namespace SCLauncher.ui.views;

public partial class Settings : UserControl
{
	
	public Settings()
	{
		InitializeComponent();
		
		var config = App.GetService<GlobalConfiguration>();
		DataContext = config;
	}
	
}