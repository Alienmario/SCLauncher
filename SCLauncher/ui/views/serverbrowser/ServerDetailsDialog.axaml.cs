using Avalonia.Controls;
using Avalonia.Input;

namespace SCLauncher.ui.views.serverbrowser;

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