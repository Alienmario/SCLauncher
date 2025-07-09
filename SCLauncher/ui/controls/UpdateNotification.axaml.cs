using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace SCLauncher.ui.controls;

public partial class UpdateNotification : UserControl
{
	private const int MsDelay = 15_000;

	public string? Url { get; set; }

	public UpdateNotification()
	{
		InitializeComponent();
		
		if (!Design.IsDesignMode)
		{
			IsVisible = false;
		}
	}

	public void Show()
	{
		if (!IsVisible)
		{
			IsVisible = true;
			Task.Delay(MsDelay).ContinueWith(_ => Dispatcher.UIThread.Post(Dismiss));
		}
	}

	public void Dismiss()
	{
		ContentControl.Content = null;
	}

	private void OnClick(object? sender, RoutedEventArgs e)
	{
		if (Url != null)
		{
			TopLevel.GetTopLevel(this)!.Launcher.LaunchUriAsync(new Uri(Url));
		}

		Dismiss();
	}

	private void OnDismissClicked(object? sender, RoutedEventArgs e)
	{
		Dismiss();
	}

	private void OnTransitionCompleted(object? sender, TransitionCompletedEventArgs e)
	{
		if (e.HasRunToCompletion && e.To == null)
			IsVisible = false;
	}
}