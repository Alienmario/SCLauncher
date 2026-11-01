using Avalonia.Controls;
using Avalonia.Interactivity;
using SCLauncher.Shared.Model.ServerInstall;
using SCLauncher.UI.Controls.Wizard;

namespace SCLauncher.UI.Views.ServerInstall;

public partial class InstallMethodSelect : UserControl, IWizardPage
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

	private void OnSteamClicked(object? sender, RoutedEventArgs e)
	{
		Advance(ServerInstallMethod.Steam);
	}

	private void OnStandaloneClicked(object? sender, RoutedEventArgs e)
	{
		Advance(ServerInstallMethod.Standalone);
	}

	private void Advance(ServerInstallMethod method)
	{
		if (DataContext is ServerInstallParams installParams)
		{
			installParams.Method = method;
		}

		if (method == ServerInstallMethod.Steam)
		{
			Wizard?.SetPageContent(new InstallOverview());
		}
		else
		{
			Wizard?.SetPageContent(new InstallPathSelect());
		}
	}
}