using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Octokit;

namespace SCLauncher.ui.controls;

public partial class UpdateOverlay : BaseOverlay
{
	private readonly IEnumerable<Release> _releases;
	private CancellationTokenSource? _cts;
	
	public UpdateOverlay() : this([]) {}

	public UpdateOverlay(IEnumerable<Release> releases)
	{
		InitializeComponent();
		_releases = releases;

		ItemsControl.ItemsSource = _releases.Select(r => new ReleaseViewModel
		{
			Title = string.IsNullOrWhiteSpace(r.Name) ? r.TagName : r.Name,
			Body = string.IsNullOrWhiteSpace(r.Body) ? "(No release notes)" : r.Body.Trim(),
			Url = r.HtmlUrl
		}).ToList();

		Closed += OnClosed;

		if (Design.IsDesignMode)
		{
			Dispatcher.Post(() =>
			{
				App.ShowInfo("Test notification", TopLevel.GetTopLevel(this));
			});
		}
	}
	
	private void OnClosed(object? sender, EventArgs e)
	{
		_cts?.Cancel();
	}

	private void OnReleaseTitleClicked(object? sender, RoutedEventArgs e)
	{
		if (sender is Control { DataContext: ReleaseViewModel vm })
		{
			TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync(new Uri(vm.Url));
		}
	}

	private async void OnUpdateNowClicked(object? sender, RoutedEventArgs e)
	{
		_cts = new CancellationTokenSource();
		UpdateButton.IsEnabled = false;
		UpdateButtonIcon.IsVisible = false;

		try
		{
			var updater = App.GetService<Updater>();
			var release = _releases.First();
			await foreach (var progressUpdate in updater.RunUpdate(release).WithCancellation(_cts.Token))
			{
				StatusText.Text = progressUpdate.Text;
			}
		}
		catch (Exception ex)
		{
			if (ex is OperationCanceledException)
				return;

			ex.Log();
			App.ShowFailure($"Update failed: {ex.Message}");

			UpdateButton.IsEnabled = true;
			UpdateButtonIcon.IsVisible = true;
			StatusText.Text = "Update now";
		}
		finally
		{
			_cts?.Dispose();
			_cts = null;
		}
	}

	public class ReleaseViewModel
	{
		public string Title { get; init; } = "";
		public string Body { get; init; } = "";
		public string Url { get; init; } = "";
	}
	
}
