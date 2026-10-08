using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SCLauncher.backend.service;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.ui.controls.wizard;

namespace SCLauncher.ui.views.serverhost;

public partial class ServerConsole : UserControl, IWizardPage
{
	private readonly ServerControlService svController;
	private readonly ClientControlService clController;
	private readonly ServerMessageAnalyzerService analyzer;
	private readonly ProfilesService profilesService;

	public ServerConsole()
	{
		InitializeComponent();
		
		svController = App.GetService<ServerControlService>();
		clController = App.GetService<ClientControlService>();
		analyzer = App.GetService<ServerMessageAnalyzerService>();
		profilesService = App.GetService<ProfilesService>();

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
		var hostname = profilesService.ActiveProfile.ServerConfig.Hostname;
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
			ConsoleViewer.AddMessage(new StatusMessage(string.Empty), true);
		}
		else if (text.Equals("clear", StringComparison.OrdinalIgnoreCase))
		{
			ConsoleViewer.Clear();
		}
		else
		{
			svController.Command(text);
			ConsoleViewer.ScrollToEnd();
		}
	}

	private void OnStartServerClicked(object? sender, RoutedEventArgs args)
	{
		CommandBar.IsOpen = false;
		svController.Start();
	}
	
	private void OnStopServerClicked(object? sender, RoutedEventArgs args)
	{
		CommandBar.IsOpen = false;
		svController.Stop();
	}
	
	private void OnMenuConnectClicked(object? sender, RoutedEventArgs args)
	{
		CommandBar.IsOpen = false;
		if (!svController.IsRunning)
		{
			App.ShowFailure("Server is not running.");
			return;
		}

		int? port = analyzer.LocalServerPort;
		if (port == null)
		{
			App.ShowFailure("Server information is not available.");
			return;
		}

		string? ip = analyzer.LocalIp ?? FindLocalIp();
		if (ip == null)
		{
			App.ShowFailure("Server information is not available.");
			return;
		}

		try
		{
			if (clController.ConnectToServer(ip + ":" + port, profilesService.ActiveProfile.ServerConfig.Password))
			{
				App.ShowSuccess("Joining local server...");
			}
			else
			{
				App.ShowFailure("Unable to launch the game.");
			}
		}
		catch (Exception e)
		{
			App.ShowFailure(e.Message);
		}
	}

	private async void OnMenuCopyPublicLinkClicked(object? sender, RoutedEventArgs args)
	{
		CommandBar.IsOpen = false;
		try
		{
			string? ip = analyzer.PublicIp;
			int? port = analyzer.PublicServerPort;
			bool isFakeIp = analyzer.IsFakeIp;
			if (ip == null || port == null)
			{
				App.ShowFailure("Server information is not available.");
				return;
			}

			var pw = profilesService.ActiveProfile.ServerConfig.Password;
			var link = SteamUtils.GetConnectLink(ip + ":" + port, profilesService.ActiveProfile.GameAppId, pw);
		
			await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(link);
			
			var delay = TimeSpan.FromSeconds(2);
			App.ShowSuccess($"Public join link{(string.IsNullOrEmpty(pw) ? "" : " (incl. password)")} copied to clipboard.",
				expiration: delay);
			await Task.Delay(delay);
			
			if (isFakeIp)
			{
				App.ShowWarning("You have Steam Networking enabled - copied link is a \"fake\" and Steam will not resolve it!" +
				                "\nFor best experience, players should join via in-game server browser.",
					expiration: TimeSpan.FromSeconds(25));
			}
			else
			{
				App.ShowInfo("Please note that port forwarding may be required." +
				             $"\nEnsure your network is not blocking port {port}.",
					expiration: TimeSpan.FromSeconds(25));
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}
	
	private async void OnMenuCopyPublicIpClicked(object? sender, RoutedEventArgs args)
	{
		CommandBar.IsOpen = false;
		try
		{
			string? ip = analyzer.PublicIp;
			int? port = analyzer.PublicServerPort;
			bool isFakeIp = analyzer.IsFakeIp;
			if (ip == null || port == null)
			{
				App.ShowFailure("Server information is not available.");
				return;
			}

			await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(ip + ":" + port);

			var delay = TimeSpan.FromSeconds(2);
			App.ShowSuccess("Public server address copied to clipboard.", expiration: delay);
			await Task.Delay(delay);
			
			if (isFakeIp)
			{
				App.ShowWarning("You have Steam networking enabled - copied IP is a \"fake\"." +
				                "\nFor best experience, players should join via in-game server browser.",
					expiration: TimeSpan.FromSeconds(25));
			}
			else
			{
				App.ShowInfo("Please note that port forwarding may be required." +
				             $"\nEnsure your network is not blocking port {port}.",
					expiration: TimeSpan.FromSeconds(25));
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private async void OnMenuCopyLocalIpClicked(object? sender, RoutedEventArgs args)
	{
		CommandBar.IsOpen = false;
		try
		{
			int? port = analyzer.LocalServerPort;
			if (port == null)
			{
				App.ShowFailure("Server information is not available.");
				return;
			}

			string? ip = analyzer.LocalIp ?? FindLocalIp();
			if (ip == null)
			{
				App.ShowFailure("Server information is not available.");
				return;
			}

			await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(ip + ":" + port);
			App.ShowSuccess("Local server address copied to clipboard.");
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private async void OnMenuBrowseServerFolderClicked(object? sender, RoutedEventArgs args)
	{
		CommandBar.IsOpen = false;
		try
		{
			if (profilesService.ActiveProfile.ServerPath == null)
				return;

			if (!await TopLevel.GetTopLevel(this)!.Launcher.LaunchDirectoryInfoAsync(
				    new DirectoryInfo(profilesService.ActiveProfile.ServerPath)))
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
		CommandBar.IsOpen = false;
		if (svController.IsRunning)
		{
			App.ShowFailure("Server has to be stopped first.");
			return;
		}
		
		if (App.MainWindow.HostServerTab.Content is HostServer hs)
		{
			hs.StartInstallWizard();
		}
	}

	private void OnMenuUninstallClicked(object? sender, RoutedEventArgs args)
	{
		CommandBar.IsOpen = false;
		if (svController.IsRunning)
		{
			App.ShowFailure("Server has to be stopped first.");
			return;
		}
		
		if (App.MainWindow.HostServerTab.Content is HostServer hs)
		{
			hs.StartUninstallWizard();
		}
	}

	private void OnMenuConfigureServerClicked(object? sender, RoutedEventArgs args)
	{
		CommandBar.IsOpen = false;
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