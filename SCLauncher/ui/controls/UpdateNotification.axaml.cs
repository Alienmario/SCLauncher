using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace SCLauncher.ui.controls;

public partial class UpdateNotification : UserControl
{

	private const int MsDelay = 15_000;
	private CancellationTokenSource? _dismissCts;

	public string? Url { get; set; }

	public bool IsOpen => TransformControl.Classes.Contains("show");

	public UpdateNotification()
	{
		InitializeComponent();
		
		if (Design.IsDesignMode)
		{
			Show();
		}
	}

	public void Show()
	{
		CancelDismissTimer();
		
		TransformControl.Classes.Add("show");
		
		_dismissCts = new CancellationTokenSource();
		var token = _dismissCts.Token;
		Task.Delay(MsDelay, token)
			.ContinueWith(t => Dispatcher.UIThread.Post(Dismiss), token,
				TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);
	}

	public void Dismiss()
	{
		CancelDismissTimer();
		TransformControl.Classes.Remove("show");
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromVisualTree(e);
		CancelDismissTimer();
	}

	private void CancelDismissTimer()
	{
		_dismissCts?.Cancel();
		_dismissCts?.Dispose();
		_dismissCts = null;
	}

	private void OnClick(object? sender, RoutedEventArgs e)
	{
		if (Url != null)
		{
			TopLevel.GetTopLevel(this)!.Launcher.LaunchUriAsync(new Uri(Url));
		}

		Dismiss();
	}

	private void OnDismissButtonClicked(object? sender, RoutedEventArgs e)
	{
		Dismiss();
	}

}