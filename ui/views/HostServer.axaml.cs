using Avalonia.Controls;
using Avalonia.Interactivity;
using SCLauncher.model;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.views.serverinstall;

namespace SCLauncher.ui.views;

public partial class HostServer : UserControl
{
	public HostServer()
	{
		InitializeComponent();

		ConfigHolder config = App.GetService<ConfigHolder>();
		SwitchContent(ServerNotFoundPanel);
		ServerWizard.CancelClick += ServerWizardCancelled;
	}

	private void LocateServerClicked(object? sender, RoutedEventArgs e)
	{
		var mainWindow = App.GetService<MainWindow>();
		mainWindow.GoToSettings();
	}

	private void InstallServerClicked(object? sender, RoutedEventArgs e)
	{
		SwitchContent(ServerWizard);
		ServerWizard.Reset();
		ServerWizard.SetContent(new MethodSelect());
		ServerWizard.DataContext = new ServerInstallParams();
	}

	private void ServerWizardCancelled(object? sender, RoutedEventArgs e)
	{
		SwitchContent(ServerNotFoundPanel);
	}

	private void SwitchContent(Control content)
	{
		foreach (var ctrl in ContentParent.Children)
		{
			ctrl.IsVisible = ctrl == content;
		}
	}
}