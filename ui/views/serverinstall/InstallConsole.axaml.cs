using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using SCLauncher.backend.service;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverinstall;

public partial class InstallConsole : UserControl, WizardNavigator.IWizardContent
{
	private bool postDetach;
	
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
		postDetach = true;
	}

	private async void RunInstaller(WizardNavigator wizard)
	{
		if (DataContext is ServerInstallParams data)
		{
			var cancellation = new CancellationTokenSource();
			EventHandler cancelOnExitHandler = (sender, args) => cancellation.Cancel();
			wizard.OnExit += cancelOnExitHandler;
			
			try
			{
				wizard.ShowProgressBar = true;
				
				var installService = App.GetService<ServerInstallService>();
				await foreach (var msg in installService.GetInstaller(data).WithCancellation(cancellation.Token))
				{
					AppendMessage(msg);
				}
			}
			catch (Exception e)
			{
				if (e is InstallException)
				{
					string message = e.GetAllMessages();
					message = "Installation failed" + (message.Length == 0 ? "" : ": " + message);
					AppendMessage(new StatusMessage(message, MessageStatus.Error));
				}
				else if (e is not OperationCanceledException)
				{
					AppendMessage(new StatusMessage(
						"Application error occured (let a dev know!)\nStack trace:\n" + e, MessageStatus.Error));
				}
			}
			finally
			{
				wizard.OnExit -= cancelOnExitHandler;
				if (!postDetach)
				{
					wizard.Completed = true;
					wizard.ShowProgressBar = false;
				}
			}
		}
	}

	private void AppendMessage(StatusMessage msg)
	{
		ConsoleViewer.AddMessage(msg);
	}
}