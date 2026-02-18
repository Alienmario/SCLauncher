using System;
using System.Linq;
using Avalonia.Interactivity;
using SCLauncher.backend.service;
using SCLauncher.model.config;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.profiles;

public partial class InitializeProfilesDialog : BaseDialogWindow
{
	private readonly ProfilesService profilesService;
	public required Action? ConfirmHandler { init; get; }

	public InitializeProfilesDialog()
	{
		InitializeComponent();
		profilesService = App.GetService<ProfilesService>();
		ProfileListBox.ItemsSource = Enum.GetValues<AppType>().Select(e => e.GetDescription());
		
		Activated += delegate
		{
			InvalidateArrange(); // SizeToContent bugfix
		};
	}
	
	private void OkClicked(object? sender, RoutedEventArgs e)
	{
		foreach (AppType appType in ProfileListBox.Selection.SelectedIndexes.Cast<AppType>())
		{
			profilesService.CreateProfile(appType, appType.GetDescription());
		}
		ConfirmHandler?.Invoke();
		Close();
	}
}
