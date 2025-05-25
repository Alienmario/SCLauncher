using System.Diagnostics;
using System.Text.RegularExpressions;
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
		if (msg.Text.StartsWith("""
		                    Unable to load plugin "addons/metamod/bin/
		                    """)
		    || msg.Text.Contains("metamod/bin/linux64/server.so: wrong ELF class: ELFCLASS64"))
		{
			msg.Details = """
			              This error message appears because Metamod includes both 32-bit and 64-bit binaries.
			              It can be safely ignored.
			              """;
		}
		
		// Not using srcds_run anymore, but same solution applies -- "srcds_run: 342: ./srcds_linux: not found"
		else if (msg.Text.StartsWith("Failed to open dedicated_srv.so"))
		{
			msg.Details = """
			              Please install necessary system packages using the following commands:
			              sudo dpkg --add-architecture i386
			              sudo apt update
			              sudo apt install lib32gcc-s1 lib32stdc++6
			              """;
		}
		
		else if (msg.Text.StartsWith("[SM] Blaming: srccoop"))
		{
			msg.Details = "Please report this error to the SourceCoop team!";
		}
		
		else if (ScBadGamedataRegex().IsMatch(msg.Text))
		{
			msg.Details = """
			              This error indicates that there might have been a game update which have broken SourceCoop.
			              
			              Check for SourceCoop updates.
			              
			              Even if there are none, the developers are probably already working on it, so check back later.
			              """;
		}
		
		ConsoleViewer.AddMessage(msg);
	}

    [GeneratedRegex(@"\[srccoop.*\.smx\] Could not obtain gamedata")]
    private static partial Regex ScBadGamedataRegex();
    
}