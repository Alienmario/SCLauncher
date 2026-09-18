using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SCLauncher.ui.views.profiles.init;

public partial class InitializeProfilesDialog : Window
{
	public required Action? ConfirmHandler { get; init; }

	public InitializeProfilesDialog()
	{
		InitializeComponent();
		
		Wizard.SetPageContent(new Page1());
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		// stabilize size
		SizeToContent = SizeToContent.Manual;
	}
	
	private void OnWizardExit(object? sender, EventArgs e)
	{
		ConfirmHandler?.Invoke();
		Close();
	}
}
