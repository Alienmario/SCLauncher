using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MarkView.Avalonia;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls;
using SCLauncher.ui.design;
using SCLauncher.ui.util;

namespace SCLauncher.ui.views.serverinstall;

public partial class VersionSelectOverlay : BaseOverlay
{
	private readonly Action<ComponentVersion?>? _onVersionSelected;
	private readonly string? _selectedVersion;
	private IReadOnlyList<ComponentVersion>? _allVersions;
	private CancellationTokenSource? _cts;
	private bool _innerClickHandled;

	/// Designer constructor
	public VersionSelectOverlay() : this(
		ServerInstallComponent.Server,
		ct => Task.FromResult<IReadOnlyList<ComponentVersion>>(
			[new DComponentVersion(1), new DComponentVersion(2), new DComponentVersion(3)]),
		null, null)
	{
		Debug.Assert(Design.IsDesignMode);
	}

	public VersionSelectOverlay(
		ServerInstallComponent component,
		Func<CancellationToken, Task<IReadOnlyList<ComponentVersion>>> versionsLoader,
		string? currentSelection,
		Action<ComponentVersion?>? onVersionSelected)
	{
		InitializeComponent();
		SetupItemClickHandling();

		_onVersionSelected = onVersionSelected;
		_selectedVersion = currentSelection;

		HeaderTextBlock.Text = component.Title;

		Closed += OnClosed;
		LoadVersions(versionsLoader);
	}

	private void OnClosed(object? sender, EventArgs args)
	{
		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;
	}

	private async void LoadVersions(Func<CancellationToken, Task<IReadOnlyList<ComponentVersion>>> loader)
	{
		_cts = new CancellationTokenSource();
		CancellationToken ct = _cts.Token;
		
		try
		{
			MainContent.VisibleChild = LoadingIndicator;

			_allVersions = await loader(ct);
			if (ct.IsCancellationRequested)
				return;

			var preReleases = _allVersions.Where(v => v.Prerelease).ToArray();
			PreReleaseToggle.IsVisible = preReleases.Any();
			if (preReleases.Any(v => v.Version.Equals(_selectedVersion, StringComparison.OrdinalIgnoreCase)))
			{
				PreReleaseToggle.IsChecked = true;
			}
			ApplyFilter();
			
			MainContent.VisibleChild = MainContentScrollViewer;
			PreReleaseToggle.IsCheckedChanged += OnFilterChanged;
			FilterTextBox.TextChanged += OnFilterChanged;
		}
		catch (Exception e)
		{
			if (e is OperationCanceledException)
				return;

			e.Log();
			if (!ct.IsCancellationRequested)
			{
				Close();
				App.ShowFailure(e.GetAllMessages());
			}
		}
	}

	private void ApplyFilter()
	{
		if (_allVersions == null)
			return;

		bool preReleaseOnly = PreReleaseToggle.IsChecked == true;
		string? filter = FilterTextBox.Text?.Trim();

		VersionsItemsControl.ItemsSource = _allVersions
			.Where(v => v.Prerelease == preReleaseOnly)
			.Where(v => string.IsNullOrEmpty(filter) ||
			            v.Version.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
			            v.Title.Contains(filter, StringComparison.OrdinalIgnoreCase))
			.Select(v => new VersionItem
			{
				ComponentVersion = v,
				Title = string.IsNullOrWhiteSpace(v.Title) ? v.Version : v.Title,
				Body = string.IsNullOrWhiteSpace(v.Description) ? "(No description)" : v.Description,
				Url = v.Url,
				DateText = v.Date?.LocalDateTime.ToShortDateString(),
				IsSelected = v.Version.Equals(_selectedVersion, StringComparison.OrdinalIgnoreCase)
			})
			.ToList();

		MainContent.VisibleChild = VersionsItemsControl.ItemCount != 0
			? MainContentScrollViewer
			: EmptyFallbackTextBlock;
	}

	private void OnFilterChanged(object? sender, RoutedEventArgs args)
	{
		ApplyFilter();
	}

	private void OnVersionTitleClicked(object? sender, RoutedEventArgs args)
	{
		if (sender is Control { DataContext: VersionItem { Url: { } url } })
		{
			TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync(new Uri(url));
		}
	}
	
	private void OnDefaultClicked(object? sender, RoutedEventArgs args)
	{
		_onVersionSelected?.Invoke(null);
		Close();
	}

	private void SetupItemClickHandling()
	{
		// Tapped is raised even if an inner control (title button, markdown link) already handled the click,
		// so track inner clicks and skip version selection for them
		VersionsItemsControl.AddHandler(PointerPressedEvent, (_, _) => _innerClickHandled = false,
			RoutingStrategies.Tunnel, handledEventsToo: true);
		VersionsItemsControl.AddHandler(Button.ClickEvent, (_, _) => _innerClickHandled = true,
			RoutingStrategies.Bubble, handledEventsToo: true);
		VersionsItemsControl.AddHandler(MarkdownViewer.LinkClickedEvent, (_, _) => _innerClickHandled = true,
			RoutingStrategies.Bubble, handledEventsToo: true);
	}

	private void OnVersionItemTapped(object? sender, TappedEventArgs args)
	{
		if (_innerClickHandled)
		{
			_innerClickHandled = false;
			return;
		}

		if (sender is Control { DataContext: VersionItem item })
		{
			_onVersionSelected?.Invoke(item.ComponentVersion);
			Close();
		}
	}
	
	public class VersionItem
	{
		public required ComponentVersion ComponentVersion { get; init; }
		public string Title { get; init; } = "";
		public string Body { get; init; } = "";
		public string? Url { get; init; }
		public string? DateText { get; init; }
		public bool IsSelected { get; init; }
	}

}
