using Avalonia.Controls;
using Avalonia.Input;

namespace SCLauncher.UI.Views.ServerBrowser;

public partial class ServerDetailsDialog : Window
{
	public ServerDetailsDialog()
	{
		InitializeComponent();
		
		KeyUp += (sender, args) =>
		{
			if (args.Key == Key.Escape)
				Close();
		};
	}
}