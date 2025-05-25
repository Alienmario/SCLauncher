using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SCLauncher.backend.service;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.views.serverinstall;

namespace SCLauncher.ui.views;

public partial class HostServer : UserControl
{
	
	public HostServer()
	{
		InitializeComponent();
		ServerInstallWizard.OnExit += OnServerInstallWizardExit;
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		Control? content = GetContent();
		if (content == null
		    || content == ServerNotFoundPanel
		    || (content == ServerConsole && !App.GetService<ServerControlService>().IsRunning))
		{
			CheckAvailability();
		}
	}
	
	private void OnLocateServerClicked(object? sender, RoutedEventArgs e)
	{
		var mainWindow = App.GetService<MainWindow>();
		mainWindow.GoToSettings();
		Settings settings = (mainWindow.SettingsTab.Content as Settings)!;
		settings.ServerPath.SelectAll();
		settings.ServerPath.Focus();
	}

	private void OnInstallServerClicked(object? sender, RoutedEventArgs e)
	{
		SwitchContent(ServerInstallWizard);
		ServerInstallWizard.Reset();
		ServerInstallWizard.SetContent(new MethodSelect());

		var installService = App.GetService<ServerInstallService>();
		ServerInstallWizard.DataContext = installService.NewInstallParams();
	}

	private void OnRecheckAvailabilityClicked(object? sender, RoutedEventArgs e)
	{
		CheckAvailability();
	}
	
	private void OnServerInstallWizardExit(object? sender, EventArgs eventArgs)
	{
		CheckAvailability();
	}

	private void SwitchContent(Control content)
	{
		foreach (var control in ContentParent.Children)
		{
			control.IsVisible = control == content;
		}
	}

	private Control? GetContent()
	{
		return ContentParent.Children.FirstOrDefault(control => control.IsVisible);
	}

	private void CheckAvailability()
	{
		SwitchContent(LoadingPanel);
		var serverControlService = App.GetService<ServerControlService>();
		var cancellationTokenSource = new CancellationTokenSource();
		var cancellationToken = cancellationTokenSource.Token;
		Task.Run(async () =>
		{
			return await serverControlService.IsAvailableAsync(cancellationToken);
		}, cancellationToken).ContinueWith(task =>
		{
			Dispatcher.UIThread.Post(() =>
			{
				if (!task.IsCompletedSuccessfully)
				{
					task.LogExceptions();
					ServerNotFoundText.Text = "There was an issue verifying current installation...";
					SwitchContent(ServerNotFoundPanel);
				}
				else if (task.Result == ServerAvailability.Available)
				{
					SwitchContent(ServerConsole);
				}
				else
				{
					ServerNotFoundText.Text = task.Result == ServerAvailability.PartiallyInstalled
						? "Some addons are not installed..."
						: "Server installation not found...";
					SwitchContent(ServerNotFoundPanel);
				}
			});
		}, cancellationToken);
	}

}