using System;
using Avalonia.Controls;

namespace SCLauncher.ui.views.profiles.init;

public partial class InitializeProfilesDialog : Window
{
	public required Action? ConfirmHandler { get; init; }

	public InitializeProfilesDialog()
	{
		InitializeComponent();
		
		Wizard.SetPageContent(new Page1());
	}

	private void OnWizardExit(object? sender, EventArgs e)
	{
		ConfirmHandler?.Invoke();
		Close();
	}
}
