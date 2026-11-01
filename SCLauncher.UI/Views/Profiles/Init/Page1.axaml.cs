using System;
using System.Linq;
using Avalonia.Controls;
using SCLauncher.Shared.Model.Definition;
using SCLauncher.UI.Controls.Wizard;

namespace SCLauncher.UI.Views.Profiles.Init;

public partial class Page1 : UserControl, IWizardPage
{
	private WizardNavigator? _wizard;
	
	public Page1()
	{
		InitializeComponent();
		
		ProfileListBox.ItemsSource = Enum.GetValues<AppType>();
		ProfileListBox.SelectionChanged += OnSelectionChanged;
	}

	private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		UpdateWizardControls();
	}

	public void OnAttachedToWizard(WizardNavigator wizard, bool unstacked)
	{
		_wizard = wizard;
		UpdateWizardControls();
	}

	public void OnDetachedFromWizard(WizardNavigator wizard, bool stacked)
	{
		_wizard = null;
	}

	public void OnNextPageRequest(WizardNavigator wizard)
	{
		wizard.PageContent = new Page2
		{
			AppTypes = ProfileListBox.SelectedItems!.Cast<AppType>()
		};
	}

	private void UpdateWizardControls()
	{
		_wizard?.SetControls(forward: ProfileListBox.Selection.Count > 0);
	}
}