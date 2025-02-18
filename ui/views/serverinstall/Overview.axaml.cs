using Avalonia.Controls;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverinstall;

public partial class Overview : UserControl, WizardNavigator.IWizardContent
{
	public Overview()
	{
		InitializeComponent();
	}
	
	public void OnAttachedToWizard(WizardNavigator wizard, bool reAttached)
	{
		wizard.SetControls(forward: true, back: true);
	}
}