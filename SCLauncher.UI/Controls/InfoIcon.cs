using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace SCLauncher.UI.Controls;

/// <summary>
/// Small info icon that displays <see cref="Text"/> in an anchored tooltip on hover.
/// </summary>
public partial class InfoIcon : PathIcon
{
	protected override Type StyleKeyOverride => typeof(InfoIcon);

	[Content]
	[GeneratedStyledProperty]
	public partial string? Text { get; set; }
}
