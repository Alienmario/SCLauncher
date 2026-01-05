using System;
using Avalonia.Controls;
using SCLauncher.backend.service;
using SCLauncher.model.config;

namespace SCLauncher.ui.views.profiles;

public partial class ProfileSwitcher : UserControl
{
	private const string ManageProfilesItem = "Manage Profiles...";
	
	private readonly BackendService backendService;

	public ProfileSwitcher()
	{
		InitializeComponent();
		backendService = App.GetService<BackendService>();

		ProfileComboBox.SelectionChanged += OnProfileSelectionChanged;

		Refresh();
		backendService.ProfileSwitched += (s, e) => SelectActiveProfile();
		backendService.ProfileAdded += (s, e) => Refresh();
		backendService.ProfileDeleted += (s, e) => Refresh();
	}

	private void Refresh()
	{
		ProfileComboBox.Items.Clear();
		foreach (var profile in backendService.Profiles)
		{
			ProfileComboBox.Items.Add(profile);
		}
		SelectActiveProfile();
		
		ProfileComboBox.Items.Add(ManageProfilesItem);
	}

	private void SelectActiveProfile()
	{
		var activeProfile = backendService.ActiveProfile;
		foreach (var item in ProfileComboBox.Items)
		{
			if (item == activeProfile)
			{
				ProfileComboBox.SelectedItem = item;
				break;
			}
		}
	}

	private void OnProfileSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ProfileComboBox.SelectedItem is string selectedString)
		{
			if (selectedString == ManageProfilesItem)
			{
				// Open ProfileManager window
				OpenProfileManager();
				// Reset selection to active profile
				SelectActiveProfile();
			}
		}
		else if (ProfileComboBox.SelectedItem is AppProfile selectedProfile)
		{
			backendService.SetActiveProfile(selectedProfile.Name);
		}
	}

	private async void OpenProfileManager()
	{
		try
		{
			var mainWindow = App.GetService<MainWindow>();
			var profileManager = new ProfileManager();
			await profileManager.ShowDialog(mainWindow);
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

}
