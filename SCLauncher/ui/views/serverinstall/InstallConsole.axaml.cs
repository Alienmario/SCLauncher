using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using SCLauncher.backend.service;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls.wizard;

namespace SCLauncher.ui.views.serverinstall;

public partial class InstallConsole : UserControl, IWizardPage
{
	private bool _postDetach;
	
	public InstallConsole()
	{
		InitializeComponent();
	}

	public void OnAttachedToWizard(WizardNavigator wizard, bool unstacked)
	{
		wizard.SetControls(forward: false, back: false);
		RunInstaller(wizard);
	}

	public void OnDetachedFromWizard(WizardNavigator wizard, bool stacked)
	{
		_postDetach = true;
	}

	private async void RunInstaller(WizardNavigator wizard)
	{
		string operation;
		IAsyncEnumerable<object> installer;
		switch (DataContext)
		{
			case ServerInstallParams sip:
				operation = "Installation";
				installer = App.GetService<ServerInstallService>().GetInstaller(sip);
				break;
			case ServerUninstallParams sup:
				operation = "Uninstallation";
				installer = App.GetService<ServerInstallService>().GetUninstaller(sup);
				break;
			default: throw new InvalidOperationException();
		}
		
		var cts = new CancellationTokenSource();
		EventHandler cancelOnExitHandler = delegate { cts.Cancel(); };
		wizard.Exit += cancelOnExitHandler;
		(wizard.NavBar as CancellableNavBar)?.ShowProgressBar = true;
		
		try
		{
			await foreach (var data in installer.WithCancellation(cts.Token))
			{
				if (data is StatusMessage msg)
				{
					AppendMessage(msg);
				}
				else if (data is ProgressUpdate update)
				{
					if (update.Text != null)
						Header.Text = update.Text;
					if (update.NumSteps != null)
						ProgressBar.Maximum = (double)update.NumSteps;
					if (update.Step != null)
						ProgressBar.Value = (double)update.Step;
				}
			}
		}
		catch (Exception e)
		{
			Header.Text = $"{operation} failed :(";
			if (e is InstallException)
			{
				AppendMessage(new StatusMessage(e.GetAllMessages(), MessageStatus.Error));
			}
			else if (e is not OperationCanceledException)
			{
				e.Log();
				AppendMessage(new StatusMessage(
					"Application error occured (let a dev know!)\nStack trace:\n" + e, MessageStatus.Error));
			}
		}
		finally
		{
			wizard.Exit -= cancelOnExitHandler;
			if (!_postDetach)
			{
				var navbar = wizard.NavBar as CancellableNavBar;
				navbar?.ShowProgressBar = false;
				navbar?.Completed = true;
			}
		}
	}

	private void AppendMessage(StatusMessage msg)
	{
		ConsoleViewer.AddMessage(msg);
	}
}