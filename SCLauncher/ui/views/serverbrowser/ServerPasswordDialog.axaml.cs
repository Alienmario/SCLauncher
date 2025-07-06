using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SCLauncher.ui.views.serverbrowser;

public partial class ServerPasswordDialog : Window
{
	public ServerPasswordDialog()
	{
		InitializeComponent();
		Activated += delegate { PasswordTextBox.Focus(); };
	}

	private void ConnectClicked(object? sender, RoutedEventArgs e)
	{
		Close(PasswordTextBox.Text);
	}
}