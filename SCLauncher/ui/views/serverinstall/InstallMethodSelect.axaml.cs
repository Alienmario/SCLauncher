using Avalonia.Controls;
using Avalonia.Interactivity;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverinstall;

public partial class InstallMethodSelect : UserControl, WizardNavigator.IWizardContent
{
	public WizardNavigator? Wizard { get; set; }

	public InstallMethodSelect()
	{
		InitializeComponent();
	}

	public void OnAttachedToWizard(WizardNavigator wizard, bool unstacked)
	{
		wizard.SetControls(forward: false, back: true);
		Wizard = wizard;
	}

	private void SteamClicked(object? sender, RoutedEventArgs e)
	{
		Advance(ServerInstallMethod.Steam);
	}

	private void ExternalClicked(object? sender, RoutedEventArgs e)
	{
		Advance(ServerInstallMethod.External);
	}

	private void Advance(ServerInstallMethod method)
	{
		if (DataContext is ServerInstallParams installParams)
		{
			installParams.Method = method;
		}

		if (method == ServerInstallMethod.Steam)
		{
			Wizard?.SetContent(new InstallOverview());
		}
		else
		{
			Wizard?.SetContent(new InstallPathSelect());
		}
	}
}