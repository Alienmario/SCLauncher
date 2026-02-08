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

public class OverviewComponentEntry
{
	public required ServerInstallComponent Component { get; init; }
	public required string Status { get; set; }
	public bool Install { get; set; }
	public bool InstallEditable { get; set; }
}

public partial class InstallOverview : UserControl, WizardNavigator.IWizardContent
{
	
	private WizardNavigator? Wizard { get; set; }
	private CancellationTokenSource? PreparationCancelSrc { get; set; }
	private ObservableCollection<OverviewComponentEntry> ComponentEntries { get; } = [];
	
	public InstallOverview()
	{
		InitializeComponent();

		ComponentsGrid.DataContext = ComponentEntries;
		
		if (Design.IsDesignMode)
		{
			ComponentEntries.Add(new OverviewComponentEntry
			{
				Component = ServerInstallComponent.Server, Status = "Ready to install", Install = true, InstallEditable = false
			});
			ComponentEntries.Add(new OverviewComponentEntry
			{
				Component = ServerInstallComponent.MetaMod, Status = "Already installed", Install = false, InstallEditable = true
			});
			ComponentEntries.Add(new OverviewComponentEntry
			{
				Component = ServerInstallComponent.SourceMod, Status = "Upgrade (x.x.x -> y.y.y)", Install = true, InstallEditable = true
			});
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
				PathText.Text = Path.Join(installParams.Path, installParams.Profile.ServerInstallFolder);
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
			installParams.Components = ComponentEntries
				.Where(i => i.Install)
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
			
			var componentInfos =
				(await installService.GatherComponentInfosAsync(installParams, true, cancellationToken))
					.OrderBy(pair => pair.Key.InstallOrder);
			
			if (cancellationToken.IsCancellationRequested)
				return;
			
			Dispatcher.UIThread.Post(() =>
			{
				if (cancellationToken.IsCancellationRequested)
					return;
				
				foreach ((ServerInstallComponent component, ComponentInfo componentInfo) in componentInfos)
				{
					string? status = null;
					if (componentInfo.Installable)
						status = "Ready to install";
					if (componentInfo.Installed)
						status = "Already installed";
					if (componentInfo.Upgradable)
					{
						status = "Upgrade";
						if (componentInfo.Version != null && componentInfo.UpgradeVersion != null)
						{
							status += $" ({componentInfo.Version} -> {componentInfo.UpgradeVersion})";
						}
					}
					
					if (status == null)
						continue;
				
					ComponentEntries.Add(new OverviewComponentEntry
					{
						Component = component,
						Status = status,
						Install = !status.Equals("Already installed"),
						InstallEditable = !status.Equals("Ready to install") || component.Optional
					});
				}
				Wizard?.SetControls(forward: true);
				LoadingIndicator.IsActive = false;
			});
		}, cancellationToken).LogExceptions();
	}
}