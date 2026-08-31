using Avalonia.Controls;
using SCLauncher.ui.controls.wizard;

namespace SCLauncher.ui.views.serverinstall;

public partial class UninstallOverview : UserControl, IWizardPage
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
		wizard.SetPageContent(new UninstallConsole());
	}

}