using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls;
using SCLauncher.ui.util;

namespace SCLauncher.ui.views.serverinstall;

public partial class VersionSelectOverlay : BaseOverlay
{
	private readonly Action<ComponentVersion?>? _onVersionSelected;
	private readonly string? _selectedVersionStr;
	private IReadOnlyList<ComponentVersion>? _allVersions;
	private CancellationTokenSource? _cts;
	private bool _isUpdatingSelection;

	public VersionSelectOverlay()
	{
		InitializeComponent();
	}

	public VersionSelectOverlay(
		ServerInstallComponent component,
		ComponentVersion? currentSelection,
		Func<CancellationToken, Task<IReadOnlyList<ComponentVersion>>> versionsLoader,
		Action<ComponentVersion?> onVersionSelected)
	{
		InitializeComponent();

		_onVersionSelected = onVersionSelected;
		_selectedVersionStr = currentSelection?.Version;

		HeaderTextBlock.Text = component.Title;
		PreReleaseToggle.IsChecked = currentSelection?.Prerelease == true;

		Closed += OnClosed;
		LoadVersions(versionsLoader);
	}

	private void OnClosed(object? sender, EventArgs args)
	{
		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;
	}

	private void LoadVersions(Func<CancellationToken, Task<IReadOnlyList<ComponentVersion>>> versionsLoader)
	{
		_cts = new CancellationTokenSource();
		CancellationToken ct = _cts.Token;

		MainContent.VisibleChild = LoadingIndicator;

		Task.Run(async () =>
		{
			try
			{
				var versions = await versionsLoader(ct);
				if (ct.IsCancellationRequested)
					return;

				Dispatcher.UIThread.Post(() =>
				{
					if (ct.IsCancellationRequested)
						return;

					MainContent.VisibleChild = VersionsListBox;
					bool hasPreReleases = versions.Any(v => v.Prerelease);
					PreReleaseToggle.IsVisible = hasPreReleases;
					if (!hasPreReleases)
					{
						PreReleaseToggle.IsChecked = false;
					}
					_allVersions = versions;
					ApplyFilter();
				});
			}
			catch (Exception e)
			{
				if (e is OperationCanceledException)
					return;

				e.Log();
				Dispatcher.UIThread.Post(() =>
				{
					if (ct.IsCancellationRequested)
						return;

					Close();
					App.ShowFailure(e.GetAllMessages());
				});
			}
		}, ct);
	}

	private void ApplyFilter()
	{
		if (_allVersions == null)
			return;

		bool preReleaseOnly = PreReleaseToggle.IsChecked == true;
		string? filter = FilterTextBox.Text?.Trim();

		var items = _allVersions
			.Where(v => v.Prerelease == preReleaseOnly)
			.Where(v => string.IsNullOrEmpty(filter) ||
			            v.Version.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
			            v.Title.Contains(filter, StringComparison.OrdinalIgnoreCase))
			.Select(v =>
			{
				bool isSelected = _selectedVersionStr != null &&
				                  string.Equals(v.Version, _selectedVersionStr, StringComparison.Ordinal);
				return new VersionItemViewModel
				{
					ComponentVersion = v,
					Title = string.IsNullOrWhiteSpace(v.Title) ? v.Version : v.Title,
					Body = string.IsNullOrWhiteSpace(v.Description) ? "(No description)" : v.Description,
					Url = v.Url,
					DateText = v.Date?.LocalDateTime.ToShortDateString(),
					IsSelected = isSelected
				};
			})
			.ToList();

		if (items.Count == 0)
		{
			MainContent.VisibleChild = EmptyText;
			return;
		}

		_isUpdatingSelection = true;
		try
		{
			VersionsListBox.ItemsSource = items;
			VersionsListBox.SelectedItem = items.FirstOrDefault(i => i.IsSelected);
			MainContent.VisibleChild = VersionsListBox;
		}
		finally
		{
			_isUpdatingSelection = false;
		}
	}

	private void OnFilterChanged(object? sender, RoutedEventArgs args)
	{
		ApplyFilter();
	}

	private void OnVersionTitleClicked(object? sender, RoutedEventArgs args)
	{
		if (sender is Control { DataContext: VersionItemViewModel { Url: { } url } })
		{
			TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync(new Uri(url));
		}
	}

	private void OnSelectionChanged(object? sender, SelectionChangedEventArgs args)
	{
		if (_isUpdatingSelection)
			return;

		var item = args.AddedItems.OfType<VersionItemViewModel>().FirstOrDefault()
			?? args.RemovedItems.OfType<VersionItemViewModel>().FirstOrDefault();
		if (item != null)
		{
			_onVersionSelected?.Invoke(item.ComponentVersion);
			Close();
		}
	}

	private void OnDefaultClicked(object? sender, RoutedEventArgs args)
	{
		_onVersionSelected?.Invoke(null);
		Close();
	}

	public class VersionItemViewModel
	{
		public required ComponentVersion ComponentVersion { get; init; }
		public string Title { get; init; } = "";
		public string Body { get; init; } = "";
		public string? Url { get; init; }
		public string? DateText { get; init; }
		public bool IsSelected { get; init; }
	}
}
