using System;
using System.IO;
using Avalonia.Controls;
using SCLauncher.backend.service;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls.wizard;

namespace SCLauncher.ui.views.serverinstall;

public partial class InstallPathSelect : UserControl, IWizardPage
{
	private WizardNavigator? _wizard;

	public InstallPathSelect()
	{
		InitializeComponent();
		
		InstallPath.TextChanged += delegate
		{
			if (_wizard?.PageContent == this)
				_wizard.SetControls(forward: IsValid());
		};
	}

	public void OnAttachedToWizard(WizardNavigator wizard, bool unstacked)
	{
		_wizard = wizard;
		wizard.SetControls(forward: IsValid(), back: true);
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
		wizard.SetPageContent(new InstallOverview());
	}

}