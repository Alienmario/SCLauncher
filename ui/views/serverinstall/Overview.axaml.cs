using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using SCLauncher.backend.service;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverinstall;

public partial class Overview : UserControl, WizardNavigator.IWizardContent
{
	private WizardNavigator? Wizard { get; set; }
	private CancellationTokenSource? PreparationCancelSrc { get; set; }
	
	public Overview()
	{
		InitializeComponent();
	}
	
	public void OnAttachedToWizard(WizardNavigator wizard, bool unstacked)
	{
		Wizard = wizard;
		wizard.SetControls(forward: false, back: true);
		wizard.ForwardButtonRunsAction = true;

		if (DataContext is ServerInstallParams data)
		{
			if (data.CreateSubfolder)
			{
				Path.Text = System.IO.Path.Join(data.Path, data.Subfolder);
			}
			else
			{
				Path.Text = data.Path;
			}

			PreparationCancelSrc = new CancellationTokenSource();
			Prepare(data, PreparationCancelSrc.Token);
		}
	}

	public void OnDetachedFromWizard(WizardNavigator wizard, bool stacked)
	{
		Wizard = null;
		PreparationCancelSrc?.Cancel();
		PreparationCancelSrc?.Dispose();
		PreparationCancelSrc = null;
	}

	public void OnNextPageRequest(WizardNavigator wizard)
	{
		wizard.SetContent(new InstallConsole());
	}

	private void Prepare(ServerInstallParams installParams, CancellationToken cancellationToken)
	{
		var installService = App.GetService<ServerInstallService>();

		Task.Run(async () =>
		{
			if (cancellationToken.IsCancellationRequested)
				return;
			
			ISet<ServerInstallComponent> components = await installService.GatherInstallableComponents(installParams);
			if (cancellationToken.IsCancellationRequested)
				return;
			
			installParams.Components = components;

			Dispatcher.UIThread.Post(() =>
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					Wizard?.SetControls(forward: true);
				}
			});
		}, cancellationToken).LogExceptions();
	}
}