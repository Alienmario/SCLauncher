using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using SCLauncher.backend.service;
using SCLauncher.model;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls;

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
		if (DataContext is ServerInstallParams data)
		{
			var cancellation = new CancellationTokenSource();
			EventHandler cancelOnExitHandler = (sender, args) => cancellation.Cancel();
			wizard.OnExit += cancelOnExitHandler;
			
			try
			{
				var installService = App.GetService<ServerInstallService>();

				await foreach (var msg in installService.GetInstaller(data).WithCancellation(cancellation.Token))
				{
					AppendMessage(msg);
				}

				wizard.Completed = true;
			}
			catch (Exception e)
			{
				if (e is not OperationCanceledException)
				{
					AppendMessage(new StatusMessage("Application error occured\n" + e, MessageStatus.Error));
				}
			}
			finally
			{
				wizard.OnExit -= cancelOnExitHandler;
			}
		}
	}

	private void AppendMessage(StatusMessage msg)
	{
		Console.AddMessage(msg);
	}
}