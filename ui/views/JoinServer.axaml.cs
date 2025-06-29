using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SCLauncher.backend.service;
using SCLauncher.model.serverbrowser;
using SCLauncher.ui.dialogs;

namespace SCLauncher.ui.views;

public partial class JoinServer : UserControl
{
	private readonly ServerBrowserService serverBrowserService;
	private readonly ClientControlService clientControlService;
	private readonly List<Server> servers = [];
	private readonly ObservableCollection<Server> filteredServers = [];
	private CancellationTokenSource? refreshCts;
	private DateTime? lastRefresh;

	public JoinServer()
	{
		InitializeComponent();
		serverBrowserService = App.GetService<ServerBrowserService>();
		clientControlService = App.GetService<ClientControlService>();
		ServerListBox.ItemsSource = filteredServers;
	}

	public async Task RefreshAsync()
	{
		try
		{
			refreshCts?.Cancel();
			refreshCts?.Dispose();
			lastRefresh = DateTime.Now;
			refreshCts = new CancellationTokenSource();
			servers.Clear();
			filteredServers.Clear();
			try
			{
				await foreach (var server in serverBrowserService.GetServers(refreshCts.Token))
				{
					servers.Add(server);
					if (PassesFilter(server))
					{
						filteredServers.Add(server);
					}
				}
			}
			catch (TaskCanceledException) {}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		HotKeyManager.SetHotKey(RefreshButton, new KeyGesture(Key.F5));
	
		if (lastRefresh == null || lastRefresh?.AddMinutes(5) < DateTime.Now)
		{
			if (!Design.IsDesignMode)
			{
				await RefreshAsync();
			}
		}
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromVisualTree(e);
		HotKeyManager.SetHotKey(RefreshButton, null!);
	}

	private async void OnRefreshClicked(object? sender, RoutedEventArgs args)
	{
		await RefreshAsync();
	}

	private void OnFilterChanged(object? sender, TextChangedEventArgs e)
	{
		filteredServers.Clear();
		foreach (var server in servers)
		{
			if (PassesFilter(server))
			{
				filteredServers.Add(server);
			}
		}
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

	private async void OnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs args)
	{
		if (args.PointerPressedEventArgs.ClickCount > 1)
		{
			if (args.Row.DataContext is Server server)
			{
				args.PointerPressedEventArgs.Handled = true;

				string? pw = null;
				if (server.Password)
				{
					var passwordDialog = new ServerPasswordDialog();
					pw = await passwordDialog.ShowDialog<string?>(App.GetService<MainWindow>());
					if (string.IsNullOrEmpty(pw))
						return;
				}
				
				clientControlService.ConnectToServer(server.Endpoint, pw);
			}
		}
	}
}