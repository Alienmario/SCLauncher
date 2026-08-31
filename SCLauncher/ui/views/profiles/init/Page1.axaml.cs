using System;
using System.Linq;
using Avalonia.Controls;
using SCLauncher.model.definition;
using SCLauncher.ui.controls.wizard;

namespace SCLauncher.ui.views.profiles.init;

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
		(wizard.NavBar as TextNavBar)?.Text = "1 / 2";
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