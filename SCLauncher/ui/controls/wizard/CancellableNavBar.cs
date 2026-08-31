using Avalonia;
using Avalonia.Controls;

namespace SCLauncher.ui.controls.wizard;

public partial class CancellableNavBar : UserControl, IWizardNavBar
{
	public static readonly StyledProperty<bool> ShowProgressBarProperty =
		AvaloniaProperty.Register<CancellableNavBar, bool>(nameof(ShowProgressBar));
	
	public static readonly StyledProperty<bool> CompletedProperty =
		AvaloniaProperty.Register<CancellableNavBar, bool>(nameof(Completed));

	public bool ShowProgressBar
	{
		get => GetValue(ShowProgressBarProperty);
		set => SetValue(ShowProgressBarProperty, value);
	}

	public bool Completed
	{
		get => GetValue(CompletedProperty);
		set => SetValue(CompletedProperty, value);
	}

	public CancellableNavBar()
	{
		InitializeComponent();
	}

	public void Reset()
	{
		ShowProgressBar = false;
		Completed = false;
	}

}