using Avalonia.Controls;
using SCLauncher.UI.Controls.Wizard;

namespace SCLauncher.UI.Views.ServerInstall;

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
		wizard.SetPageContent(new InstallConsole());
	}

}