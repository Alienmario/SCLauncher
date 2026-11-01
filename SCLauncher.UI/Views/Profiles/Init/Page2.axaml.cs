using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using SCLauncher.Shared.Extensions;
using SCLauncher.Shared.Model.Definition;
using SCLauncher.Shared.Service;
using SCLauncher.UI.Controls.Wizard;

namespace SCLauncher.UI.Views.Profiles.Init;

public partial class Page2 : UserControl, IWizardPage
{
	public partial class Item : ObservableObject
	{
		public required AppType AppType { get; init; }
		public required IEnumerable<AppPreset> Presets { get; init; }
		[ObservableProperty]
		public required partial AppPreset SelectedPreset { get; set; }
	}

	public IEnumerable<AppType> AppTypes
	{
		set
		{
			DataContext = value.Order().Select(appType =>
			{
				var availablePresets = AppDefinitions.Get(appType).AvailablePresets;
				return new Item { AppType = appType, Presets = availablePresets, SelectedPreset = availablePresets[0] };
			}).ToImmutableArray();
		}
	}
	
	public Page2()
	{
		InitializeComponent();
		if (Design.IsDesignMode)
		{
			AppTypes = Enum.GetValues<AppType>();
		}
	}

	public void OnAttachedToWizard(WizardNavigator wizard, bool unstacked)
	{
		wizard.ForwardButtonRunsAction = true;

		if (DataContext is IEnumerable<Item> items && !items.Any(item => item.Presets.Count() > 1))
		{
			CreateProfiles(items);
			wizard.RequestExit();
		}
	}

	public void OnNextPageRequest(WizardNavigator wizard)
	{
		if (DataContext is IEnumerable<Item> items)
		{
			CreateProfiles(items);
			wizard.RequestExit();
		}
	}

	private static void CreateProfiles(IEnumerable<Item> items)
	{
		var profilesService = App.GetService<IProfilesService>();
		foreach (var item in items.OrderBy(item => item.AppType))
		{
			profilesService.CreateProfile(item.AppType, item.SelectedPreset, item.AppType.GetDescription());
		}
	}
}