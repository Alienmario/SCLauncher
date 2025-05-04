using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using SCLauncher.backend.service;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.serverinstall;

public partial class Overview : UserControl, WizardNavigator.IWizardContent
{
	private WizardNavigator? Wizard { get; set; }
	private CancellationTokenSource? PreparationCancelSrc { get; set; }
	private ObservableCollection<OverviewComponentEntry> InstallComponents { get; } = [];
	
	public Overview()
	{
		InitializeComponent();

		ComponentsGrid.DataContext = InstallComponents;
		
		if (Design.IsDesignMode)
		{
			InstallComponents.Add(new OverviewComponentEntry { Component = ServerInstallComponent.Server, Status = "Install" });
			InstallComponents.Add(new OverviewComponentEntry { Component = ServerInstallComponent.SourceMod, Status = "Upgrade from x.x.x" });
		}
	}
	
	public void OnAttachedToWizard(WizardNavigator wizard, bool unstacked)
	{
		Wizard = wizard;
		wizard.SetControls(forward: false, back: true);
		wizard.ForwardButtonRunsAction = true;

		if (DataContext is ServerInstallParams installParams)
		{
			if (installParams.CreateSubfolder)
			{
				PathText.Text = Path.Join(installParams.Path, installParams.Subfolder);
			}
			else
			{
				PathText.Text = installParams.Path;
			}

			PreparationCancelSrc = new CancellationTokenSource();
			Prepare(installParams, PreparationCancelSrc.Token);
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
		if (DataContext is ServerInstallParams installParams)
		{
			installParams.Components = InstallComponents
				.Select(i => i.Component)
				.ToHashSet();
		}
		wizard.SetContent(new InstallConsole());
	}

	private void Prepare(ServerInstallParams installParams, CancellationToken cancellationToken)
	{
		var installService = App.GetService<ServerInstallService>();

		Task.Run(async () =>
		{
			if (cancellationToken.IsCancellationRequested)
				return;
			
			var installableComponents = await installService.GatherInstallableComponents(installParams);
			
			if (cancellationToken.IsCancellationRequested)
				return;
			
			Dispatcher.UIThread.Post(() =>
			{
				if (cancellationToken.IsCancellationRequested)
					return;
				
				foreach ((ServerInstallComponent component, ComponentInfo? componentInfo) in installableComponents)
				{
					var status = componentInfo != null
						? componentInfo.Version != null ? $"Upgrade ({componentInfo.Version} -> Latest)" : "Upgrade"
						: "Install";
				
					InstallComponents.Add(new OverviewComponentEntry
					{
						Component = component,
						Status = status
					});
				}
				Wizard?.SetControls(forward: true);
			});
		}, cancellationToken).LogExceptions();
	}
}