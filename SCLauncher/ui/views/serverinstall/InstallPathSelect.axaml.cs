using System;
using System.IO;
using Avalonia.Controls;
using SCLauncher.model.config;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverinstall;

public partial class InstallPathSelect : UserControl, WizardNavigator.IWizardContent
{
	public InstallPathSelect()
	{
		InitializeComponent();
		
		DataContextChanged += (sender, args) =>
		{
			if (DataContext is ServerInstallParams p && string.IsNullOrWhiteSpace(p.Path))
			{
				var configPath = App.GetService<GlobalConfiguration>().ServerPath;
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
		if (Directory.Exists(InstallPath.Text))
			return true;
		
		try
		{
			Path.GetFullPath(InstallPath.Text!);
		}
		catch (Exception)
		{
			return false;
		}

		return Path.IsPathFullyQualified(InstallPath.Text!);
	}

	public void OnNextPageRequest(WizardNavigator wizard)
	{
		wizard.SetContent(new InstallOverview());
	}

}