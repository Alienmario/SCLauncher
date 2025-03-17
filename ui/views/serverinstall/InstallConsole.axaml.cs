using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using SCLauncher.backend.service;
using SCLauncher.model;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls;
using SCLauncher.ui.util;

namespace SCLauncher.ui.views.serverinstall;

public partial class InstallConsole : UserControl, WizardNavigator.IWizardContent
{
	public InstallConsole()
	{
		InitializeComponent();
	}

	public void OnAttachedToWizard(WizardNavigator wizard, bool unstacked)
	{
		wizard.SetControls(forward: false, back: false);
		RunInstaller(wizard);
	}

	private async void RunInstaller(WizardNavigator wizard)
	{
		try
		{
			if (DataContext is ServerInstallParams data)
			{
				var installService = App.GetService<ServerInstallService>();
				var cancellation = new CancellationTokenSource();
				wizard.OnExit += (sender, args) => cancellation.Cancel();
				
				await foreach (var msg in installService.GetInstaller(data).WithCancellation(cancellation.Token))
				{
					AppendMessage(msg);
				}

				wizard.Completed = true;
			}
		}
		catch (Exception e)
		{
			AppendMessage(new StatusMessage("Application error occured\n" + e, MessageStatus.Error));
		}
	}

	private void AppendMessage(StatusMessage msg)
	{
		ConsoleTextUtils.AppendMessage(Console, Scroller, msg);
	}
}