using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using SCLauncher.backend.service;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls.wizard;

namespace SCLauncher.ui.views.serverinstall;

public partial class UninstallConsole : UserControl, IWizardPage
{
	private bool _postDetach;
	
	public UninstallConsole()
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
		if (DataContext is ServerUninstallParams data)
		{
			var cancellation = new CancellationTokenSource();
			EventHandler cancelOnExitHandler = (sender, args) => cancellation.Cancel();
			wizard.Exit += cancelOnExitHandler;
			
			try
			{
				CancellableNavBar navbar = (CancellableNavBar)wizard.NavBar!;
				navbar.ShowProgressBar = true;
				
				var installService = App.GetService<ServerInstallService>();
				await foreach (var msg in installService.GetUninstaller(data).WithCancellation(cancellation.Token))
				{
					AppendMessage(msg);
				}
			}
			catch (Exception e)
			{
				if (e is InstallException)
				{
					string message = e.GetAllMessages();
					message = "Uninstallation failed" + (message.Length == 0 ? "" : ": " + message);
					AppendMessage(new StatusMessage(message, MessageStatus.Error));
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
					CancellableNavBar navbar = (CancellableNavBar)wizard.NavBar!;
					navbar.ShowProgressBar = false;
					navbar.Completed = true;
				}
			}
		}
	}

	private void AppendMessage(StatusMessage msg)
	{
		ConsoleViewer.AddMessage(msg);
	}
}