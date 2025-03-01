using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using SCLauncher.backend.service;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverinstall;

public partial class InstallStatus : UserControl, WizardNavigator.IWizardContent
{
	public InstallStatus()
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
				wizard.Cancelled += (sender, args) => cancellation.Cancel();
				
				await foreach (var msg in installService.RunInstaller(data).WithCancellation(cancellation.Token))
				{
					AppendMessage(msg);
				}
			}
		}
		catch (Exception e)
		{
			AppendMessage(new ServerInstallMessage("Application error occured\n" + e));
		}
	}

	private void AppendMessage(ServerInstallMessage msg)
	{
		Console.Text += $"{msg.Time:HH:mm:ss}  {msg.Text}\n";
		if (Scroller.Offset.NearlyEquals(Scroller.ScrollBarMaximum))
		{
			Scroller.ScrollToEnd();
		}
	}
}