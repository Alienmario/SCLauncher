using Avalonia.Controls;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverinstall;

public partial class Overview : UserControl, WizardNavigator.IWizardContent
{
	public Overview()
	{
		InitializeComponent();
	}
	
	public void OnAttachedToWizard(WizardNavigator wizard, bool unstacked)
	{
		wizard.SetControls(forward: true, back: true);
		wizard.ForwardButtonRunsAction = true;

		if (DataContext is ServerInstallParams data)
		{
			if (data.CreateSubfolder)
			{
				Path.Text = System.IO.Path.Join(data.Path, data.Subfolder);
			}
			else
			{
				Path.Text = data.Path;
			}
		}
	}

	public void OnNextPageRequest(WizardNavigator wizard)
	{
		wizard.SetContent(new InstallStatus());
	}
}