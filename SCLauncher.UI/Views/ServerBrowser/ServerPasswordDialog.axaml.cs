using Avalonia.Input;
using Avalonia.Interactivity;
using SCLauncher.UI.Controls;

namespace SCLauncher.UI.Views.ServerBrowser;

public partial class ServerPasswordDialog : BaseDialogWindow
{
	public ServerPasswordDialog() : this(true) {}
	
	public ServerPasswordDialog(bool required)
	{
		InitializeComponent();
		Title = required ? "Password is required" : "Enter password or leave empty";
		Activated += delegate
		{
			PasswordTextBox.Focus();
		};
		KeyUp += (sender, args) =>
		{
			if (args.Key == Key.Escape)
				Close();
		};
	}

	private void ConnectClicked(object? sender, RoutedEventArgs e)
	{
		Close(PasswordTextBox.Text ?? string.Empty);
	}
}