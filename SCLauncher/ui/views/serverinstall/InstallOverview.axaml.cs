using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SCLauncher.backend.service;
using SCLauncher.model.definition;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;
using SCLauncher.ui.controls.wizard;

namespace SCLauncher.ui.views.serverinstall;

public partial class InstallOverview : UserControl, IWizardPage
{
	
	private WizardNavigator? Wizard { get; set; }
	private CancellationTokenSource? GatherCancelSrc { get; set; }
	private ObservableCollection<ComponentViewModel> ComponentEntries { get; } = [];

	public bool IsLoading
	{
		get => LoadingIndicator.IsActive;
		private set
		{
			LoadingIndicator.IsActive = value;
			ComponentsScrollViewer.IsVisible = !value;
		}
	}

	public InstallOverview()
	{
		InitializeComponent();

		ComponentsGrid.DataContext = ComponentEntries;
		
		if (Design.IsDesignMode)
		{
			ComponentEntries.Add(new ComponentViewModel
			{
				Component = ServerInstallComponent.Server,
				ComponentInfo = ComponentInfo.ReadyToInstall,
				Action = ComponentViewModel.CheckAction.Install
			});
			ComponentEntries.Add(new ComponentViewModel
			{
				Component = ServerInstallComponent.MetaMod,
				ComponentInfo = new ComponentInfo(),
				Action = ComponentViewModel.CheckAction.Reinstall
			});
			ComponentEntries.Add(new ComponentViewModel
			{
				Component = ServerInstallComponent.SourceMod,
				ComponentInfo = new ComponentInfo { Version = "1.0", Upgradable = true, UpgradeVersion = "2.0" },
				Action = ComponentViewModel.CheckAction.Update
			});
			foreach (ComponentViewModel entry in ComponentEntries)
			{
				entry.PropertyChanged += OnComponentPropertyChanged;
			}
			ResetInstallStates();
		}
	}

	protected override void OnDataContextChanged(EventArgs args)
	{
		base.OnDataContextChanged(args);
		if (Wizard != null && GatherCancelSrc == null && DataContext is ServerInstallParams installParams)
		{
			Setup(installParams);
		}
	}

	public void OnAttachedToWizard(WizardNavigator wizard, bool unstacked)
	{
		Wizard = wizard;
		wizard.ForwardButtonRunsAction = true;
		
		if (DataContext is ServerInstallParams installParams)
		{
			Setup(installParams);
		}
	}

	public void OnDetachedFromWizard(WizardNavigator wizard, bool stacked)
	{
		Wizard = null;
		GatherCancelSrc?.Cancel();
		GatherCancelSrc?.Dispose();
		GatherCancelSrc = null;
	}

	private void Setup(ServerInstallParams installParams)
	{
		Wizard?.SetControls(forward: false, back: true);
		
		// Construct install path text
		if (installParams.CreateSubfolder)
		{
			InstallPathText.Text = Path.Join(installParams.Path, installParams.Profile.ServerInstallFolder);
		}
		else
		{
			InstallPathText.Text = installParams.Path;
		}

		// Prepare presets combo
		var availablePresets = AppDefinitions.Get(installParams.Profile.AppType).AvailablePresets;
		PresetComboBox.SelectionChanged -= OnPresetSelectionChanged;
		PresetComboBox.ItemsSource = availablePresets.Count == 1
			? availablePresets
			: (List<AppPreset?>)[null, .. availablePresets]; // if count > 1, prepend a 'select all' item
		PresetComboBox.SelectedItem = installParams.Profile.AppPreset;
		PresetComboBox.SelectionChanged += OnPresetSelectionChanged;

		GatherComponentData(installParams);
	}

	public void OnNextPageRequest(WizardNavigator wizard)
	{
		if (DataContext is ServerInstallParams installParams)
		{
			installParams.Components = ComponentEntries
				.Where(i => i.Enabled)
				.Select(i => i.Component)
				.ToHashSet();
		}
		wizard.SetPageContent(new InstallConsole());
	}

	private void GatherComponentData(ServerInstallParams installParams)
	{
		var installService = App.GetService<ServerInstallService>();
		
		GatherCancelSrc?.Cancel();
		GatherCancelSrc?.Dispose();
		GatherCancelSrc = new CancellationTokenSource();
		CancellationToken ct = GatherCancelSrc.Token;
		
		IsLoading = true;

		Task.Run(async () =>
		{
			if (ct.IsCancellationRequested)
				return;
			
			var componentInfos = (await installService.GatherComponentInfosAsync(installParams, true, ct))
					.OrderBy(pair => pair.Key.InstallOrder)
					.ToList();
			
			if (ct.IsCancellationRequested)
				return;
			
			Dispatcher.UIThread.Post(() =>
			{
				if (ct.IsCancellationRequested)
					return;
				
				// Remove old component entries
				foreach (var entry in ComponentEntries)
				{
					entry.PropertyChanged -= OnComponentPropertyChanged;
				}
				ComponentEntries.Clear();
				
				// Add new component entries
				foreach ((ServerInstallComponent component, ComponentInfo componentInfo) in componentInfos)
				{
					ComponentViewModel.CheckAction? action = null;

					if (componentInfo.Installable)
					{
						action = ComponentViewModel.CheckAction.Install;
					}
					if (componentInfo.Installed)
					{
						action = ComponentViewModel.CheckAction.Reinstall;
					}
					if (componentInfo.Upgradable)
					{
						action = ComponentViewModel.CheckAction.Update;
					}
					
					if (action == null)
						continue;
					
					var entry = new ComponentViewModel
					{
						Component = component,
						ComponentInfo = componentInfo,
						Action = (ComponentViewModel.CheckAction)action
					};
					entry.PropertyChanged += OnComponentPropertyChanged;
					ComponentEntries.Add(entry);
				}
				
				// Entries added, now setup their default checkbox states + dependency locks
				// and enable 'next screen' navigation if any get selected by default
				ResetInstallStates();
				IsLoading = false;
			});
		}, ct).LogExceptions();
	}

	private void OnPresetSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		ResetInstallStates();
	}

	/// Prevents re-entrance loops from property-changed events
	private bool _isUpdatingComponentStates;
	
	/// Sets components' default checkbox states by the preset
	private void ResetInstallStates()
	{
		if (_isUpdatingComponentStates) return;
		_isUpdatingComponentStates = true;

		try
		{
			foreach (var entry in ComponentEntries)
			{
				entry.Enabled = entry.Action != ComponentViewModel.CheckAction.Reinstall;
				if (entry.Action == ComponentViewModel.CheckAction.Install)
				{
					AppPreset? preset = (AppPreset?)PresetComboBox.SelectedItem;
					if (preset != null)
					{
						entry.Enabled = entry.Component.AppPresets.Contains((AppPreset)preset);
					}
				}
			}
		}
		finally
		{
			_isUpdatingComponentStates = false;
			UpdateDependencyLocks();
		}
	}

	private void UpdateDependencyLocks()
	{
		if (_isUpdatingComponentStates) return;
		_isUpdatingComponentStates = true;

		try
		{
			var forcedComponents = new HashSet<ServerInstallComponent>();
			var queue = new Queue<ServerInstallComponent>(
				ComponentEntries.Where(e => e.Enabled).Select(e => e.Component)
			);

			while (queue.TryDequeue(out var comp))
			{
				foreach (var dep in comp.Dependencies)
				{
					if (forcedComponents.Add(dep))
					{
						queue.Enqueue(dep);
					}
				}
			}

			foreach (var entry in ComponentEntries)
			{
				bool isForced = entry.Action == ComponentViewModel.CheckAction.Install && forcedComponents.Contains(entry.Component);
				if (isForced)
				{
					entry.Enabled = true;
					entry.Editable = false;
				}
				else
				{
					entry.Editable = entry.Action != ComponentViewModel.CheckAction.Install || !entry.Component.Required;
				}
			}
		}
		finally
		{
			_isUpdatingComponentStates = false;
		}
	}

	private void OnComponentPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ComponentViewModel.Enabled))
		{
			Wizard?.SetControls(forward: ComponentEntries.Any(entry => entry.Enabled));
			UpdateDependencyLocks();
		}
	}
}

public partial class ComponentViewModel : ObservableObject
{
	public enum CheckAction
	{
		Install, Reinstall, Update
	}
	
	public required ServerInstallComponent Component { get; init; }
	public required ComponentInfo ComponentInfo { get; init; }
	public required CheckAction Action
	{
		get;
		init
		{
			field = value;
			UpdateText();
		}
	}
	
	[ObservableProperty]
	public partial string? Text { get; private set; }
	
    [ObservableProperty]
    public partial bool Enabled { get; set; }

    [ObservableProperty]
    public partial bool Editable { get; set; }

    partial void OnEnabledChanged(bool value)
    {
	    UpdateText();
    }

    private void UpdateText()
    {
	    Text = Action switch
	    {
		    CheckAction.Install => Enabled ? "Install" : "Available",
		    CheckAction.Reinstall => Enabled ? "Force reinstall" : "Installed",
		    CheckAction.Update when Enabled => ComponentInfo is { Version: not null, UpgradeVersion: not null }
			    ? $"Update ({ComponentInfo.Version} -> {ComponentInfo.UpgradeVersion})"
			    : "Update",
		    CheckAction.Update => "Update available",
		    _ => "???"
	    };
    }
}