using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SCLauncher.backend.service;
using SCLauncher.model;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverhost;

public partial class ServerConsole : UserControl, WizardNavigator.IWizardContent
{
	private readonly ServerControlService serverControl;
	
	public ServerConsole()
	{
		InitializeComponent();
		serverControl = App.GetService<ServerControlService>();

		StartButton.Click += (sender, args) =>
		{
			serverControl.Start();
		};
		StopButton.Click += (sender, args) =>
		{
			serverControl.Stop();
		};
		serverControl.StateChanged += OnServerStateChanged;
		serverControl.OutputReceived += OnServerOutputReceived;
		serverControl.ErrorReceived += OnServerErrorReceived;
		
		OnServerStateChanged(null, false);
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
		});
	}

	private void SubmitButton_OnClick(object? sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(CommandTextBox.Text))
		{
			serverControl.Command(CommandTextBox.Text);
		}

		CommandTextBox.Text = null;
	}

	private void AppendMessage(StatusMessage msg)
	{
		ConsoleViewer.AddMessage(msg);
	}
	
}