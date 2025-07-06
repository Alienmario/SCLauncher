using System;
using Avalonia;
using Avalonia.Controls;

namespace SCLauncher.ui.controls;

public partial class BadgeButton : Button
{
	protected override Type StyleKeyOverride => typeof(Button);
	
	public static readonly StyledProperty<object?> CopySourceProperty =
		AvaloniaProperty.Register<BadgeButton, object?>(nameof(CopySource));

	public object? CopySource
	{
		get => GetValue(CopySourceProperty);
		set => SetValue(CopySourceProperty, value);
	}
	
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