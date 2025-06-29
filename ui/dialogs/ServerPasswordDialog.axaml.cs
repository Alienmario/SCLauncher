using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SCLauncher.ui.dialogs;

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