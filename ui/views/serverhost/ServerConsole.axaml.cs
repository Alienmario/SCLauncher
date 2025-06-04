using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SCLauncher.backend.service;
using SCLauncher.backend.util;
using SCLauncher.model;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverhost;

public partial class ServerConsole : UserControl, WizardNavigator.IWizardContent
{
	private readonly ServerControlService svController;
	private readonly ClientControlService clController;
	private readonly ServerMessageAnalyzerService analyzer;
	private readonly BackendService backend;
	private WindowNotificationManager? notificationMgr;
	
	public ServerConsole()
	{
		InitializeComponent();
		
		svController = App.GetService<ServerControlService>();
		clController = App.GetService<ClientControlService>();
		analyzer = App.GetService<ServerMessageAnalyzerService>();
		backend = App.GetService<BackendService>();

		StartButton.Click += (sender, args) =>
		{
			svController.Start();
		};
		StopButton.Click += (sender, args) =>
		{
			svController.Stop();
		};
		svController.StateChanged += OnServerStateChanged;
		svController.OutputReceived += OnServerOutputReceived;
		svController.ErrorReceived += OnServerErrorReceived;
		
		OnServerStateChanged(this, false);
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs args)
	{
		base.OnAttachedToVisualTree(args);
		notificationMgr ??= new WindowNotificationManager(TopLevel.GetTopLevel(this))
		{
			Position = NotificationPosition.BottomCenter
		};
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
		Dispatcher.UIThread.Post(() =>
		{
			StatusIndicatorLabel.Content = running ? "Online" : "Offline";
			StatusIndicator.Classes.Replace([running ? "online" : "offline"]);
			StartButton.IsVisible = !running;
			StopButton.IsVisible = running;
		});
	}

	private void OnCommandSubmit(object? sender, string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			ConsoleViewer.AddMessage(new StatusMessage(string.Empty));
		}
		else
		{
			svController.Command(text);
		}
	}

	private void OnMenuJoinClicked(object? sender, RoutedEventArgs e)
	{
		if (!svController.IsRunning)
		{
			ShowFailure("Server is not running.");
			return;
		}
		
		if (analyzer.ServerPort == null)
		{
			ShowFailure("Server information is not available.");
			return;
		}

		string? localIp = analyzer.LocalIp ?? FindLocalIp();
		if (localIp == null)
		{
			ShowFailure("Server information is not available.");
			return;
		}

		if (clController.ConnectToServer(localIp + ":" + analyzer.ServerPort))
		{
			ShowSuccess("Joining local server...");
		}
		else
		{
			ShowFailure("Unable to launch the game.");
		}
	}

	private async void OnMenuCopyLinkClicked(object? sender, RoutedEventArgs args)
	{
		try
		{
			if (analyzer.PublicIp == null || analyzer.ServerPort == null)
			{
				ShowFailure("Server information is not available.");
				return;
			}

			var link = SteamUtils.GetConnectLink(analyzer.PublicIp + ":" + analyzer.ServerPort,
				backend.ActiveApp.GameAppId);
		
			await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(link);
			ShowSuccess("Public join link copied to clipboard.");
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
				ShowFailure("Server information is not available.");
				return;
			}

			var ip = analyzer.PublicIp + ":" + analyzer.ServerPort;
			await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(ip);
			ShowSuccess("Public IP copied to clipboard.");
		}
		catch (Exception e)
		{
			e.Log();
		}
	}
	
	private void OnMenuInstallerClicked(object? sender, RoutedEventArgs e)
	{
		if (svController.IsRunning)
		{
			ShowFailure("Server has to be stopped first.");
			return;
		}
		
		if (App.GetService<MainWindow>().HostServerTab.Content is HostServer hs)
		{
			hs.GoToServerInstallWizard();
		}
	}

	private void ShowSuccess(string msg)
	{
		notificationMgr?.Show(new Notification(
			"Success", msg, NotificationType.Information, TimeSpan.FromSeconds(3))
		);
	}

	private void ShowFailure(string msg)
	{
		notificationMgr?.Show(new Notification(
			"Failed", msg, NotificationType.Error, TimeSpan.FromSeconds(4))
		);
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