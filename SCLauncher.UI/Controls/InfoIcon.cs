using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace SCLauncher.UI.Controls;

/// <summary>
/// Small info icon that displays <see cref="Text"/> in an anchored tooltip on hover.
/// </summary>
public class InfoIcon : PathIcon
{
	protected override Type StyleKeyOverride => typeof(InfoIcon);

	public static readonly StyledProperty<string?> TextProperty =
		AvaloniaProperty.Register<InfoIcon, string?>(nameof(Text));

	[Content]
	public string? Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}
}
