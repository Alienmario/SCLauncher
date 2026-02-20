using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using SCLauncher.model;

namespace SCLauncher.ui.controls;

public partial class StatusMessageViewer : UserControl
{
	public static readonly StyledProperty<bool> DisplayTimeProperty =
		AvaloniaProperty.Register<StatusMessageViewer, bool>(nameof(DisplayTime), defaultValue: true);
	
	public new ObservableCollection<StatusMessage>? DataContext
	{
		get => (ObservableCollection<StatusMessage>?) base.DataContext;
		set => base.DataContext = value;
	}

	public bool DisplayTime
	{
		get => GetValue(DisplayTimeProperty);
		set => SetValue(DisplayTimeProperty, value);
	}

	public int Limit { get; set; } = int.MaxValue;
	
	public StatusMessageViewer()
	{
		InitializeComponent();
		
		if (!Design.IsDesignMode)
			DataContext = [];

		Scroller.ScrollChanged += Scroller_ScrollChanged;
	}

	public void AddMessage(StatusMessage message, bool scroll = true, bool jumpScroll = false)
	{
		bool wasScrolledToMax = Scroller.Offset.NearlyEquals(Scroller.ScrollBarMaximum);
		
		if (DataContext != null)
		{
			DataContext.Add(message);
			if (DataContext.Count > Limit)
			{
				DataContext.RemoveAt(0);
			}
		}
		
		if (scroll && (jumpScroll || wasScrolledToMax))
		{
			UpdateLayout();
			ScrollToEnd();
		}
	}

	public void Clear() => DataContext?.Clear();
	
	public void ScrollToEnd() => Scroller.ScrollToEnd();

	private void Scroller_ScrollChanged(object? sender, ScrollChangedEventArgs args)
	{
		ScrollToBottomButton.IsVisible = !Scroller.Offset.NearlyEquals(Scroller.ScrollBarMaximum);
	}

	private void ScrollToBottomButton_Click(object? sender, RoutedEventArgs e)
	{
		ScrollToEnd();
	}

	private void ScrollToBottomButton_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
	{
		// This sucks big time
		Scroller.Offset = Scroller.Offset.WithY(Scroller.Offset.Y - e.Delta.Y * 50);
	}

	private void Message_PointerTapped(object? sender, TappedEventArgs args)
	{
		if (sender is Control ctl && ctl.DataContext is StatusMessage msg && msg.Details != null)
		{
			FlyoutBase.ShowAttachedFlyout(ctl);
		}
	}
}