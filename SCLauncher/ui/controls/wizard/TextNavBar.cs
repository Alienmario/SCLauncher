using Avalonia;
using Avalonia.Controls;
namespace SCLauncher.ui.controls.wizard;

public partial class TextNavBar : UserControl, IWizardNavBar
{
	public static readonly StyledProperty<string?> TextProperty =
		AvaloniaProperty.Register<TextNavBar, string?>(nameof(Text));

	public string? Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	public TextNavBar()
	{
		InitializeComponent();
	}
	
	public void Reset()
	{
	}
}