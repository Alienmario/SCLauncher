using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SCLauncher.backend.service;
using SCLauncher.model.serverbrowser;
using SCLauncher.ui.design;
using SCLauncher.ui.views.serverbrowser;

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
		ServerGrid.ItemsSource = filteredServers;
	}

	public async Task RefreshAsync()
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

	protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs args)
	{
		try
		{
			base.OnAttachedToVisualTree(args);
			HotKeyManager.SetHotKey(RefreshButton, new KeyGesture(Key.F5));
	
			if (lastRefresh == null || lastRefresh?.AddMinutes(5) < DateTime.Now)
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

	private async Task JoinAsync(Server server)
	{
		string? pw = null;
		if (server.Password)
		{
			var passwordDialog = new ServerPasswordDialog();
			pw = await passwordDialog.ShowDialog<string?>(App.GetService<MainWindow>());
			if (string.IsNullOrEmpty(pw))
				return;
		}

		clientControlService.ConnectToServer(server.Endpoint, pw);
		App.ShowSuccess("Joining server...");
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

	private void OnFilterTextChanged(object? sender, TextChangedEventArgs args)
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

	private async void OnServerGridCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs args)
	{
		try
		{
			var pointProperties = args.PointerPressedEventArgs.GetCurrentPoint(null).Properties;

			if (args.PointerPressedEventArgs.ClickCount > 1 && pointProperties.IsLeftButtonPressed)
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
	
	private async void OnDetailBtnClicked(object? sender, RoutedEventArgs args)
	{
		try
		{
			if (sender is Button { Content: string content })
			{
				await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(content);
				App.ShowSuccess("Copied to clipboard.");
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
		if (ServerGrid.SelectedItem is Server server)
		{
			var detailsDialog = new ServerDetailsDialog
			{
				DataContext = server
			};
			detailsDialog.Show();
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
	
}