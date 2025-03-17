using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SCLauncher.backend.service;
using SCLauncher.ui.views.serverinstall;

namespace SCLauncher.ui.views;

public partial class HostServer : UserControl
{
	public HostServer()
	{
		InitializeComponent();

		SwitchContent(ServerNotFoundPanel);
		ServerWizard.OnExit += ServerWizardOnExit;
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

		var installService = App.GetService<ServerInstallService>();
		ServerWizard.DataContext = installService.NewInstallParams();
	}

	private void ServerWizardOnExit(object? sender, EventArgs eventArgs)
	{
		SwitchContent(ServerNotFoundPanel);
	}

	private void SwitchContent(Control content)
	{
		foreach (var control in ContentParent.Children)
		{
			control.IsVisible = control == content;
		}
	}
}