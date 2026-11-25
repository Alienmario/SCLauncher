using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace SCLauncher.UI.Controls;

public partial class BadgeButton : Button
{
	protected override Type StyleKeyOverride => typeof(Button);
	
	[GeneratedStyledProperty]
	public partial object? CopySource { get; set; }
	
	public BadgeButton()
	{
		InitializeComponent();
		
		Click += async (sender, args) =>
		{
			object? src = CopySource ?? Content;
			if (src != null)
			{
				await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(src.ToString());
				App.ShowSuccess("Copied to clipboard.");
			}
		};
	}

}