using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SCLauncher.model;

namespace SCLauncher.ui.controls;

public partial class StatusMessageViewer : UserControl
{
	public static readonly StyledProperty<bool> DisplayTimeProperty =
		AvaloniaProperty.Register<StatusMessageViewer, bool>(nameof(DisplayTime), defaultValue: true);
	
	public static readonly StyledProperty<bool> CanMinimizeProperty =
		AvaloniaProperty.Register<StatusMessageViewer, bool>(nameof(CanMinimize), defaultValue: false);
	
	public static readonly StyledProperty<bool> IsExpandedProperty =
		AvaloniaProperty.Register<StatusMessageViewer, bool>(nameof(IsExpanded), defaultValue: false);
	
	public static readonly StyledProperty<double> MinMinimizedHeightProperty =
		AvaloniaProperty.Register<StatusMessageViewer, double>(nameof(MinMinimizedHeight), defaultValue: 22);
	
	public static readonly StyledProperty<double> MaxMinimizedHeightProperty =
		AvaloniaProperty.Register<StatusMessageViewer, double>(nameof(MaxMinimizedHeight), defaultValue: 66);
	
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
	
	public bool CanMinimize
	{
		get => GetValue(CanMinimizeProperty);
		set => SetValue(CanMinimizeProperty, value);
	}
	
	public bool IsExpanded
	{
		get => GetValue(IsExpandedProperty);
		set => SetValue(IsExpandedProperty, value);
	}
	
	public double MinMinimizedHeight
	{
		get => GetValue(MinMinimizedHeightProperty);
		set => SetValue(MinMinimizedHeightProperty, value);
	}
	
	public double MaxMinimizedHeight
	{
		get => GetValue(MaxMinimizedHeightProperty);
		set => SetValue(MaxMinimizedHeightProperty, value);
	}

	public int Limit { get; set; } = int.MaxValue;

	private bool _autoScroll = true;
	
	public StatusMessageViewer()
	{
		InitializeComponent();
		
		if (!Design.IsDesignMode)
			DataContext = [];

		Scroller.ScrollChanged += OnScrollChanged;
		PointerEntered += OnPointerEntered;
		PointerExited += OnPointerExited;
	}

	public void AddMessage(StatusMessage message, bool scroll = false)
	{
		if (DataContext != null)
		{
			DataContext.Add(message);
			if (DataContext.Count > Limit)
			{
				DataContext.RemoveAt(0);
			}
		}
		
		if (CanMinimize && !IsExpanded)
		{
			ScrollToEnd();
			UpdateMinimizedHeight();
		}
		else if (scroll || _autoScroll)
		{
			ScrollToEnd();
		}
	}

	public void Clear() => DataContext?.Clear();
	
	public void ScrollToEnd()
	{
		_autoScroll = true;
		Scroller.ScrollToEnd();
		ScrollToBottomButton.IsVisible = false;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		
		if (change.Property == CanMinimizeProperty || change.Property == IsExpandedProperty)
		{
			UpdateMinimizedState();
		}
	}
	
	private void UpdateMinimizedState()
	{
		if (ItemsControl == null) // May be called before controls are initialized
			return;
		
		ScrollToEnd();
		if (CanMinimize && !IsExpanded)
		{
			ScrollToBottomButton.IsVisible = false;
			Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
			UpdateMinimizedHeight();
		}
		else
		{
			Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			MaxHeight = this.GetPresentationSource()?.RootVisual?.Bounds.Height ?? double.PositiveInfinity;
		}
	}

	private void UpdateMinimizedHeight()
	{
		ItemsControl.UpdateLayout();
		var lastItem = ItemsControl.ContainerFromIndex(ItemsControl.Items.Count - 1);

		double height = 0.0;
		if (lastItem != null)
		{
			height = lastItem.DesiredSize.Height + 5;
		}
		MaxHeight = double.Clamp(height, MinMinimizedHeight, MaxMinimizedHeight);
	}

	private void OnScrollChanged(object? sender, ScrollChangedEventArgs args)
	{
		if (!CanMinimize || IsExpanded)
		{
			// Quick-scroll button is only visible when we're not scrolled to max
			ScrollToBottomButton.IsVisible = !Scroller.Offset.NearlyEquals(Scroller.ScrollBarMaximum);
		}
		
		if (args.ViewportDelta.Y != 0)
		{
			// viewport size changed
			if (_autoScroll)
			{
				ScrollToEnd();
			}
			return;
		}
		_autoScroll = Scroller.Offset.NearlyEquals(Scroller.ScrollBarMaximum);
	}

	private void OnScrollToBottomClicked(object? sender, RoutedEventArgs e)
	{
		ScrollToEnd();
	}

	private void OnScrollToBottomPointerWheelChanged(object? sender, PointerWheelEventArgs e)
	{
		// This sucks big time, simulates scrolling
		Scroller.Offset = Scroller.Offset.WithY(Scroller.Offset.Y - e.Delta.Y * 50);
	}

	private void OnMessageTapped(object? sender, TappedEventArgs args)
	{
		// Suppress detail flyout when minimized
		if (CanMinimize && !IsExpanded)
			return;
		
		if (sender is Control { DataContext: StatusMessage { Details: not null } } ctl)
		{
			FlyoutBase.ShowAttachedFlyout(ctl);
		}
	}

	private void OnPointerEntered(object? sender, PointerEventArgs args)
	{
		if (CanMinimize && !IsExpanded)
		{
			IsExpanded = true;
		}
	}

	private void OnPointerExited(object? sender, PointerEventArgs args)
	{
		if (CanMinimize && IsExpanded && args.Pointer.Captured == null)
		{
			IsExpanded = false;
		}
	}

}