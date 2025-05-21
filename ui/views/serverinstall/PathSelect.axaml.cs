using System.IO;
using Avalonia.Controls;
using SCLauncher.model;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverinstall;

public partial class PathSelect : UserControl, WizardNavigator.IWizardContent
{
	public PathSelect()
	{
		InitializeComponent();
		
		DataContextChanged += (sender, args) =>
		{
			if (DataContext is ServerInstallParams p && string.IsNullOrWhiteSpace(p.Path))
			{
				var configPath = App.GetService<ConfigHolder>().ServerPath;
				if (!string.IsNullOrWhiteSpace(configPath))
				{
					p.Path = configPath;
					p.CreateSubfolder = false;
				}
			}
		};
	}

	public void OnAttachedToWizard(WizardNavigator wizard, bool unstacked)
	{
		wizard.SetControls(forward: IsValid(), back: true);
		
		if (unstacked)
			return;

		InstallPath.TextChanged += (sender, args) =>
		{
			if (wizard.GetContent() == this)
				wizard.SetControls(forward: IsValid());
		};
	}

	private bool IsValid()
	{
		return Directory.Exists(InstallPath.Text);
	}

	public void OnNextPageRequest(WizardNavigator wizard)
	{
		wizard.SetContent(new Overview());
	}

}