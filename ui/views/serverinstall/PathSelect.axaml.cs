using Avalonia.Controls;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverinstall;

public partial class PathSelect : UserControl, WizardNavigator.IWizardContent
{
	public PathSelect()
	{
		InitializeComponent();
	}

	public void OnAttachedToWizard(WizardNavigator wizard, bool reAttached)
	{
		wizard.SetControls(forward: IsValid(), back: true);
		
		if (reAttached)
			return;

		InstallPath.TextChanged += (sender, args) =>
		{
			wizard.SetControls(forward: IsValid());
		};
	}
	
	private bool IsValid()
	{
		return !string.IsNullOrWhiteSpace(InstallPath.Text);
	}

	public void OnNextPageRequest(WizardNavigator wizard)
	{
		wizard.SetContent(new Overview());
	}

}