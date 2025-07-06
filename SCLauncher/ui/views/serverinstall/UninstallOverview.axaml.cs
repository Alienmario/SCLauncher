using Avalonia.Controls;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverinstall;

public partial class UninstallOverview : UserControl, WizardNavigator.IWizardContent
{
	
	public UninstallOverview()
	{
		InitializeComponent();
	}
	
	public void OnAttachedToWizard(WizardNavigator wizard, bool unstacked)
	{
		wizard.ForwardButtonRunsAction = true;
	}

	public void OnNextPageRequest(WizardNavigator wizard)
	{
		wizard.SetContent(new UninstallConsole());
	}

}