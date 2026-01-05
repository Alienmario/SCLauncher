using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace SCLauncher.ui.controls;

/// <summary>
/// A base pre-styled dialog window with main content area and a footer area.
/// </summary>
public class BaseDialogWindow : Window
{
	protected override Type StyleKeyOverride => typeof(BaseDialogWindow);

	public static readonly StyledProperty<object?> MainContentProperty =
		AvaloniaProperty.Register<BaseDialogWindow, object?>(nameof(MainContent));

	public static readonly StyledProperty<object?> FooterContentProperty =
		AvaloniaProperty.Register<BaseDialogWindow, object?>(nameof(FooterContent));

	public static readonly StyledProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
		AvaloniaProperty.Register<BaseDialogWindow, ScrollBarVisibility>(nameof(VerticalScrollBarVisibility));

	public object? MainContent
	{
		get => GetValue(MainContentProperty);
		set => SetValue(MainContentProperty, value);
	}

	public object? FooterContent
	{
		get => GetValue(FooterContentProperty);
		set => SetValue(FooterContentProperty, value);
	}
	
	public ScrollBarVisibility VerticalScrollBarVisibility
	{
		get => GetValue(VerticalScrollBarVisibilityProperty);
		set => SetValue(VerticalScrollBarVisibilityProperty, value);
	}
	
}
