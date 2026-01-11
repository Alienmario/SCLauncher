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
	private readonly ServerControlService serverControlService;
	private readonly ServerInstallService serverInstallService;
	private readonly ProfilesService profilesService;
	private CancellationTokenSource? cancellationTokenSource;
	
	public HostServer()
	{
		InitializeComponent();
		serverControlService = App.GetService<ServerControlService>();
		serverInstallService = App.GetService<ServerInstallService>();
		profilesService = App.GetService<ProfilesService>();
		ServerInstallWizard.OnExit += OnServerInstallWizardExit;
		ServerUninstallWizard.OnExit += OnServerInstallWizardExit;
		profilesService.ProfileSwitched += (s, e) => Dispatcher.UIThread.Post(CheckAvailability);
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		Control? content = GetContent();
		if (content == null
		    || content == ServerNotFoundPanel
		    || (content == ServerConsole && !serverControlService.IsRunning))
		{
			CheckAvailability();
		}
	}

	public void GoToServerInstallWizard()
	{
		SwitchContent(ServerInstallWizard);
		ServerInstallWizard.Reset();
		ServerInstallWizard.SetContent(new InstallMethodSelect());
		ServerInstallWizard.DataContext = serverInstallService.NewInstallParams();
	}

	public void GoToServerUninstallWizard()
	{
		SwitchContent(ServerUninstallWizard);
		ServerUninstallWizard.Reset();
		ServerUninstallWizard.SetContent(new UninstallOverview());
		ServerUninstallWizard.DataContext = serverInstallService.NewUninstallParams();
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
		GoToServerInstallWizard();
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
		cancellationTokenSource?.Cancel();
		cancellationTokenSource?.Dispose();
		cancellationTokenSource = new CancellationTokenSource();
		var ct = cancellationTokenSource.Token;

		SwitchContent(LoadingPanel);
		
		Task.Run(async () => await serverControlService.IsAvailableAsync(ct), ct).ContinueWith(task =>
		{
			Dispatcher.UIThread.Post(() =>
			{
				if (ct.IsCancellationRequested)
					return;
				
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
		}, ct);
	}

}