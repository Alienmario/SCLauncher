using Avalonia.Input;
using Avalonia.Interactivity;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverbrowser;

public partial class ServerPasswordDialog : BaseDialogWindow
{
	public ServerPasswordDialog() : this(true) {}
	
	public ServerPasswordDialog(bool required)
	{
		InitializeComponent();
		Title = required ? "Password required" : "Password";
		Activated += delegate
		{
			InvalidateArrange(); // SizeToContent bugfix
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