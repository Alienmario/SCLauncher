using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace SCLauncher.UI.Controls;

/// <summary>
/// A base pre-styled dialog window with main content area and a footer area.
/// </summary>
public partial class BaseDialogWindow : Window
{
	protected override Type StyleKeyOverride => typeof(BaseDialogWindow);

	[GeneratedStyledProperty]
	public partial object? MainContent { get; set; }

	[GeneratedStyledProperty]
	public partial object? FooterContent { get; set; }

	[GeneratedStyledProperty]
	public partial ScrollBarVisibility VerticalScrollBarVisibility { get; set; }

	[GeneratedStyledProperty(DefaultValue = true)]
	public partial bool ApplyDropShadow { get; set; }
}
