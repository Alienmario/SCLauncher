using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SCLauncher.backend.service;
using SCLauncher.backend.util;
using SCLauncher.model.config;
using SCLauncher.model.serverbrowser;
using SCLauncher.ui.design;
using SCLauncher.ui.views.serverbrowser;

namespace SCLauncher.ui.views;

public partial class JoinServer : UserControl
{
	
	private const int DirectQueryInputDelay = 1000;

	private readonly ServerBrowserService serverBrowserService;
	private readonly ClientControlService clientControlService;
	private readonly ProfilesService profilesService;
	private readonly List<Server> servers = [];
	private readonly ObservableCollection<Server> filteredServers = [];
	private CancellationTokenSource? refreshCts;
	private DateTime? lastRefresh;
	private string? directQueryEndpoint;
	private string? directQueryPassword;

	public static readonly DirectProperty<JoinServer, bool> IsRefreshingProperty =
		AvaloniaProperty.RegisterDirect<JoinServer, bool>(nameof(IsRefreshing), o => o.IsRefreshing);

	public bool IsRefreshing
	{
		get;
		private set => SetAndRaise(IsRefreshingProperty, ref field, value);
	}

	public JoinServer()
	{
		InitializeComponent();
		serverBrowserService = App.GetService<ServerBrowserService>();
		clientControlService = App.GetService<ClientControlService>();
		profilesService = App.GetService<ProfilesService>();
		var globalConfig = App.GetService<GlobalConfiguration>();

		ApiKeyButton.DataContext = globalConfig;
		ServerGrid.ItemsSource = filteredServers;
		
		profilesService.ProfileSwitched += OnProfileSwitched;
	}

	private void OnProfileSwitched(object? sender, AppProfile profile)
	{
		Dispatcher.UIThread.Post(async () =>
		{
			Reset();
			if (VisualRoot != null)
			{
				await RefreshAsync();
			}
		});
	}

	public void Reset()
	{
		if (refreshCts != null)
		{
			refreshCts.Cancel();
			refreshCts.Dispose();
			refreshCts = null;
		}
		lastRefresh = null;
		IsRefreshing = false;
		servers.Clear();
		filteredServers.Clear();
	}

	public async Task RefreshAsync(int msDelay = 0)
	{
		if (refreshCts != null)
		{
			await refreshCts.CancelAsync();
			refreshCts.Dispose();
		}

		refreshCts = new CancellationTokenSource();
		CancellationToken ct = refreshCts.Token;
		IsRefreshing = true;
		
		try
		{
			await Task.Delay(msDelay, ct);
			lastRefresh = DateTime.Now;
			if (directQueryEndpoint != null)
			{
				await RefreshDirectAsync(ct);
			}
			else
			{
				await RefreshMasterAsync(ct);
			}
		}
		catch (OperationCanceledException) {}
		catch (Exception e)
		{
			e.Log();
		}
		finally
		{
			IsRefreshing = false;
		}
	}

	private async Task RefreshMasterAsync(CancellationToken ct)
	{
		servers.Clear();
		filteredServers.Clear();
		await foreach (var server in serverBrowserService.GetServers(ct))
		{
			servers.Add(server);
			if (PassesFilter(server))
			{
				filteredServers.Add(server);
			}
		}
	}
	
	private async Task RefreshDirectAsync(CancellationToken ct)
	{
		if (directQueryEndpoint == null)
			return;
		
		filteredServers.Clear();
		var server = await serverBrowserService.QueryServer(directQueryEndpoint, ct: ct);
		if (server != null) filteredServers.Add(server);
	}
	
	private bool PassesFilter(Server server)
	{
		string? filterText = FilterTextBox.Text;
		if (string.IsNullOrWhiteSpace(filterText))
			return true;

		filterText = filterText.Trim();

		if (server.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase))
			return true;
		
		if (server.Keywords.Contains(filterText, StringComparison.OrdinalIgnoreCase))
			return true;
		
		if (server.Map.Contains(filterText, StringComparison.OrdinalIgnoreCase))
			return true;
		
		if (server.Endpoint.Contains(filterText, StringComparison.OrdinalIgnoreCase))
			return true;

		return false;
	}
	
	private async void OnFilterTextChanged(object? sender, TextChangedEventArgs args)
	{
		try
		{
			// Check for IP:port or steam://connect/ pattern
			string? filterText = FilterTextBox.Text?.Trim();
			if (!string.IsNullOrWhiteSpace(filterText))
			{
				if (TryParseEndpoint(filterText, out var ip, out var port, out var pw))
				{
					directQueryEndpoint = ip + ':' + port;
					directQueryPassword = pw;
					await RefreshAsync(DirectQueryInputDelay);
					return;
				}
			}

			directQueryEndpoint = null;
			directQueryPassword = null;
			filteredServers.Clear();
			foreach (var server in servers)
			{
				if (PassesFilter(server))
				{
					filteredServers.Add(server);
				}
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs args)
	{
		try
		{
			base.OnAttachedToVisualTree(args);
			HotKeyManager.SetHotKey(RefreshButton, new KeyGesture(Key.F5));
	
			if (lastRefresh == null || lastRefresh?.AddMinutes(5) < DateTime.Now
			    || (servers.Count == 0 && directQueryEndpoint == null))
			{
				if (Design.IsDesignMode)
				{
					filteredServers.Add(DServer.Instance);
					return;
				}
				await RefreshAsync();
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs args)
	{
		base.OnDetachedFromVisualTree(args);
		HotKeyManager.SetHotKey(RefreshButton, null!);
	}

	private async void OnRefreshClicked(object? sender, RoutedEventArgs args)
	{
		try
		{
			await RefreshAsync();
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private async Task JoinAsync(Server server)
	{
		// unless certain that the server is password-free, request a password from the user
		string? pw = null;
		if (server.Password != false)
		{
			if (directQueryPassword != null)
			{
				pw = directQueryPassword;
			}
			else
			{
				if (server.Password is null)
				{
					App.ShowInfo("Refreshing server info...");
					Server? newServer = await serverBrowserService.QueryServer(server.Endpoint);
					if (newServer != null)
					{
						server = newServer;
					}
					else App.ShowFailure($"Unable to retrieve details for {server.Name}");
				}
				if (server.Password != false)
				{
					var passwordDialog = new ServerPasswordDialog(server.Password == true);
					if (null == (pw = await passwordDialog.ShowDialog<string?>(App.GetService<MainWindow>())))
					{
						return; // window closed
					}
				}
			}
		}

		try
		{
			if (clientControlService.ConnectToServer(server.Endpoint, pw))
			{
				App.ShowSuccess("Joining server...");
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
	
	private async void OnGridCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs args)
	{
		try
		{
			if (args.PointerPressedEventArgs.ClickCount > 1
			    && args.PointerPressedEventArgs.GetCurrentPoint(null).Properties.IsLeftButtonPressed
			    && args.PointerPressedEventArgs.KeyModifiers == KeyModifiers.None)
			{
				if (args.Row.DataContext is Server server)
				{
					args.PointerPressedEventArgs.Handled = true;
					await JoinAsync(server);
				}
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}
	
	private void ContextMenu_OnOpening(object? sender, CancelEventArgs args)
	{
		if (ServerGrid.SelectedItems.Count != 1)
		{
			args.Cancel = true;
		}
	}

	private async void ContextMenu_OnViewDetails(object? sender, RoutedEventArgs args)
	{
		try
		{
			if (ServerGrid.SelectedItem is Server server)
			{
				Server? newServer = await serverBrowserService.QueryServer(server.Endpoint, true, true);
				if (newServer != null)
				{
					var detailsDialog = new ServerDetailsDialog
					{
						DataContext = newServer
					};
					detailsDialog.Show(App.GetService<MainWindow>());
				}
				else App.ShowFailure($"Unable to retrieve details for {server.Name}");
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private async void ContextMenu_OnJoinNow(object? sender, RoutedEventArgs args)
	{
		try
		{
			if (ServerGrid.SelectedItem is Server server)
			{
				await JoinAsync(server);
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private async void ContextMenu_OnCopyJoinLink(object? sender, RoutedEventArgs args)
	{
		try
		{
			if (ServerGrid.SelectedItem is Server server)
			{
				var link = SteamUtils.GetConnectLink(server.Endpoint, profilesService.ActiveProfile.GameAppId);
				await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(link);
				App.ShowSuccess("Server join link copied to clipboard.");
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}
	
	private async void ContextMenu_OnCopyIP(object? sender, RoutedEventArgs args)
	{
		try
		{
			if (ServerGrid.SelectedItem is Server server)
			{
				await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(server.Endpoint);
				App.ShowSuccess("Server IP copied to clipboard.");
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private void OnGenerateKeyClicked(object? sender, RoutedEventArgs e)
	{
		var mainWindow = App.GetService<MainWindow>();
		mainWindow.GoToSettings();
		Settings settings = (mainWindow.SettingsTab.Content as Settings)!;
		mainWindow.Launcher.LaunchUriAsync(settings.SteamApiKeyLink.NavigateUri!);
		settings.SteamApiKey.Focus();
		settings.SteamApiKey.SelectAll();
	}

	[GeneratedRegex(@"^steam://connect/(?<host>[^\.\s]+(?:\.[^\s\?/:]+)+)(?::(?<port>\d{1,5}))?(?:/(?<pw>[^\?]*))?", RegexOptions.IgnoreCase)]
	private static partial Regex SteamConnectRegex();

	[GeneratedRegex(@"^(?<host>\d{1,3}(?:\.\d{1,3}){3})(?::(?<port>\d{1,5}))?", RegexOptions.IgnoreCase)]
	private static partial Regex IpRegex();

	private static bool TryParseEndpoint(string input, out string ip, out int port, out string? pw)
	{
		ip = string.Empty; port = 0; pw = null;
		
		var match = SteamConnectRegex().Match(input);
		if (!match.Success)
			match = IpRegex().Match(input);
		if (!match.Success)
			return false;
		
		ip = match.Groups["host"].Value;
		pw = match.Groups["pw"].Success ? match.Groups["pw"].Value : null;
		if (!int.TryParse(match.Groups["port"].Value, out port))
			port = 27015;
		
		return true;
	}
}