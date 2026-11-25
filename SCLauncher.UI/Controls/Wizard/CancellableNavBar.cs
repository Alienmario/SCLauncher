using Avalonia;
using Avalonia.Controls;

namespace SCLauncher.UI.Controls.Wizard;

public partial class CancellableNavBar : UserControl, IWizardNavBar
{
	[GeneratedStyledProperty]
	public partial bool ShowProgressBar { get; set; }

	[GeneratedStyledProperty]
	public partial bool Completed { get; set; }

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