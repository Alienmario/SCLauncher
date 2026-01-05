using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SCLauncher.backend.service;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.model.exception;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverhost;

public partial class ServerConsole : UserControl, WizardNavigator.IWizardContent
{
	private readonly ServerControlService svController;
	private readonly ClientControlService clController;
	private readonly ServerMessageAnalyzerService analyzer;
	private readonly BackendService backend;

	public ServerConsole()
	{
		InitializeComponent();
		
		svController = App.GetService<ServerControlService>();
		clController = App.GetService<ClientControlService>();
		analyzer = App.GetService<ServerMessageAnalyzerService>();
		backend = App.GetService<BackendService>();

		StartButton.Click += OnStartServerClicked;
		StopButton.Click += OnStopServerClicked;
		svController.StateChanged += OnServerStateChanged;
		svController.OutputReceived += OnServerOutputReceived;
		svController.ErrorReceived += OnServerErrorReceived;
		
		OnServerStateChanged(this, false);
	}

	private void OnServerErrorReceived(object sender, DataReceivedEventArgs args)
	{
		if (args.Data != null)
		{
			Dispatcher.UIThread.Post(() => { AppendMessage(new StatusMessage(args.Data, MessageStatus.Error)); });
		}
	}

	private void OnServerOutputReceived(object sender, DataReceivedEventArgs args)
	{
		if (args.Data != null)
		{
			Dispatcher.UIThread.Post(() => { AppendMessage(new StatusMessage(args.Data)); });
		}
	}

	private void OnServerStateChanged(object? sender, bool running)
	{
		var hostname = backend.ActiveProfile.ServerConfig.Hostname;
		Dispatcher.UIThread.Post(() =>
		{
			StatusIndicatorLabel.Text = running ? "Online - " + hostname : "Offline";
			StatusIndicator.Classes.Replace([running ? "online" : "offline"]);
			StartButton.IsVisible = !running;
			StopButton.IsVisible = running;
		});
	}

	private void OnCommandSubmit(object? sender, string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			ConsoleViewer.AddMessage(new StatusMessage(string.Empty), jumpScroll: true);
		}
		else
		{
			svController.Command(text);
			ConsoleViewer.ScrollToEnd();
		}
	}

	private void OnStartServerClicked(object? sender, RoutedEventArgs args)
	{
		svController.Start();
	}
	
	private void OnStopServerClicked(object? sender, RoutedEventArgs args)
	{
		svController.Stop();
	}
	
	private void OnMenuJoinClicked(object? sender, RoutedEventArgs args)
	{
		if (!svController.IsRunning)
		{
			App.ShowFailure("Server is not running.");
			return;
		}
		
		if (analyzer.ServerPort == null)
		{
			App.ShowFailure("Server information is not available.");
			return;
		}

		string? localIp = analyzer.LocalIp ?? FindLocalIp();
		if (localIp == null)
		{
			App.ShowFailure("Server information is not available.");
			return;
		}

		try
		{
			if (clController.ConnectToServer(localIp + ":" + analyzer.ServerPort, backend.ActiveProfile.ServerConfig.Password))
			{
				App.ShowSuccess("Joining local server...");
			}
			else
			{
				App.ShowFailure("Unable to launch the game.");
			}
		}
		catch (InvalidGamePathException)
		{
			App.ShowFailure("Configured game path is invalid.");
		}
	}

	private async void OnMenuCopyLinkClicked(object? sender, RoutedEventArgs args)
	{
		try
		{
			if (analyzer.PublicIp == null || analyzer.ServerPort == null)
			{
				App.ShowFailure("Server information is not available.");
				return;
			}

			var link = SteamUtils.GetConnectLink(analyzer.PublicIp + ":" + analyzer.ServerPort,
				backend.ActiveProfile.GameAppId, backend.ActiveProfile.ServerConfig.Password);
		
			await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(link);
			App.ShowSuccess("Public join link copied to clipboard.");
		}
		catch (Exception e)
		{
			e.Log();
		}
	}
	
	private async void OnMenuCopyIpClicked(object? sender, RoutedEventArgs args)
	{
		try
		{
			if (analyzer.PublicIp == null || analyzer.ServerPort == null)
			{
				App.ShowFailure("Server information is not available.");
				return;
			}

			var ip = analyzer.PublicIp + ":" + analyzer.ServerPort;
			await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(ip);
			App.ShowSuccess("Public IP copied to clipboard.");
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private async void OnMenuBrowseServerFolderClicked(object? sender, RoutedEventArgs args)
	{
		try
		{
			if (backend.ActiveProfile.ServerPath == null)
				return;

			if (!await TopLevel.GetTopLevel(this)!.Launcher.LaunchDirectoryInfoAsync(
				    new DirectoryInfo(backend.ActiveProfile.ServerPath)))
			{
				App.ShowFailure("Unable to open server directory.");
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}
	
	private void OnMenuInstallClicked(object? sender, RoutedEventArgs args)
	{
		if (svController.IsRunning)
		{
			App.ShowFailure("Server has to be stopped first.");
			return;
		}
		
		if (App.GetService<MainWindow>().HostServerTab.Content is HostServer hs)
		{
			hs.GoToServerInstallWizard();
		}
	}

	private void OnMenuUninstallClicked(object? sender, RoutedEventArgs args)
	{
		if (svController.IsRunning)
		{
			App.ShowFailure("Server has to be stopped first.");
			return;
		}
		
		if (App.GetService<MainWindow>().HostServerTab.Content is HostServer hs)
		{
			hs.GoToServerUninstallWizard();
		}
	}

	private void OnMenuToggleTimeDisplayClicked(object? sender, RoutedEventArgs args)
	{
		ConsoleViewer.DisplayTime = !ConsoleViewer.DisplayTime;
	}
	
	private void OnMenuConfigureServerClicked(object? sender, RoutedEventArgs args)
	{
		ConfiguratorSplitView.IsPaneOpen = !ConfiguratorSplitView.IsPaneOpen;
	}
	
	private void OnConfiguratorCloseClicked(object? sender, RoutedEventArgs e)
	{
		ConfiguratorSplitView.IsPaneOpen = false;
	}
	
	private void OnConfiguratorResetClicked(object? sender, RoutedEventArgs e)
	{
		ServerConfigurator.ResetToDefaults();
		ResetServerConfigButton?.Flyout?.Hide();
	}

	private void AppendMessage(StatusMessage msg)
	{
		msg.Details = analyzer.AnalyzeMessage(msg.Text);
		ConsoleViewer.AddMessage(msg);
	}

	private static string? FindLocalIp()
	{
		return NetworkInterface
			.GetAllNetworkInterfaces()
			.Where(n => n.OperationalStatus == OperationalStatus.Up &&
			            n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
			.SelectMany(n => n.GetIPProperties().UnicastAddresses)
			.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork &&
			            !IPAddress.IsLoopback(a.Address) &&
			            !a.Address.ToString().StartsWith("169.254.")) // Exclude APIPA
			.Select(a => a.Address.ToString())
			.FirstOrDefault();
	}
}