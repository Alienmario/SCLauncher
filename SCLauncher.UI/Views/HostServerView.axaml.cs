using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SCLauncher.Shared.Model.ServerInstall;
using SCLauncher.Shared.Service;
using SCLauncher.UI.Controls.Wizard;
using SCLauncher.UI.Views.ServerInstall;

namespace SCLauncher.UI.Views;

public partial class HostServerView : UserControl
{
	internal record LoadingViewModel;
	internal record ServerNotFoundViewModel(string Message);
	internal record InstallWizardViewModel(ServerInstallParams Params);
	internal record UninstallWizardViewModel(ServerUninstallParams Params);
	internal record ServerConsoleViewModel;
	
	private readonly IServerControlService _serverControlService;
	private readonly IServerInstallService _serverInstallService;
	private readonly IProfilesService _profilesService;
	private CancellationTokenSource? _cancellationTokenSource;
	
	public HostServerView()
	{
		InitializeComponent();
		_serverControlService = App.GetService<IServerControlService>();
		_serverInstallService = App.GetService<IServerInstallService>();
		_profilesService = App.GetService<IProfilesService>();
		_profilesService.ProfileSwitched += delegate { Dispatcher.UIThread.Post(CheckAvailability); };
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs args)
	{
		base.OnAttachedToVisualTree(args);
		if (Design.IsDesignMode)
		{
			DataContext = new LoadingViewModel();
			return;
		}

		if (DataContext == null
		    || DataContext is ServerNotFoundViewModel
		    || (DataContext is ServerConsoleViewModel && !_serverControlService.IsRunning))
		{
			CheckAvailability();
		}
	}

	public void StartInstallWizard()
	{
		var installParams = _serverInstallService.NewInstallParams();
		DataContext = new InstallWizardViewModel(installParams);
		
		if (installParams is { Method: not null, Path: not null })
		{
			WizardNavigator? wizard = this.FindDescendantOfType<WizardNavigator>();
			if (installParams.Method == ServerInstallMethod.Steam)
			{
				wizard?.FastForward(new InstallOverview());
			}
			else
			{
				wizard?.FastForward(new InstallPathSelect(), new InstallOverview());
			}
		}
	}

	public void StartUninstallWizard()
	{
		DataContext = new UninstallWizardViewModel(_serverInstallService.NewUninstallParams());
	}

	private void OnLocateServerClicked(object? sender, RoutedEventArgs e)
	{
		var mainWindow = App.MainWindow;
		mainWindow.GoToSettings();
		SettingsView settings = (mainWindow.SettingsTab.Content as SettingsView)!;
		settings.ServerPath.SelectAll();
		settings.ServerPath.Focus();
	}

	private void OnInstallServerClicked(object? sender, RoutedEventArgs e)
	{
		StartInstallWizard();
	}

	private void OnRecheckAvailabilityClicked(object? sender, RoutedEventArgs e)
	{
		CheckAvailability();
	}
	
	private void OnInstallWizardExit(object? sender, EventArgs eventArgs)
	{
		CheckAvailability();
	}

	private void CheckAvailability()
	{
		_cancellationTokenSource?.Cancel();
		_cancellationTokenSource?.Dispose();
		_cancellationTokenSource = new CancellationTokenSource();
		var ct = _cancellationTokenSource.Token;

		DataContext = new LoadingViewModel();
		
		Task.Run(async () => await _serverInstallService.GetAvailabilityAsync(ct), ct).ContinueWith(task =>
		{
			Dispatcher.UIThread.Post(() =>
			{
				if (ct.IsCancellationRequested)
					return;
				
				if (!task.IsCompletedSuccessfully)
				{
					task.LogExceptions();
					DataContext = new ServerNotFoundViewModel("There was an issue verifying current installation...");
					return;
				}
				
				DataContext = task.Result switch
				{
					ServerAvailability.Available => new ServerConsoleViewModel(),
					ServerAvailability.PartiallyInstalled => new ServerNotFoundViewModel("Server found, please install required components"),
					_ => new ServerNotFoundViewModel("Server installation not found...")
				};
			});
		}, ct);
	}

}