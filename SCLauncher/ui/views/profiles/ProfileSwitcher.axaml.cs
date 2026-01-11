using System;
using Avalonia.Controls;
using Avalonia.Threading;
using SCLauncher.backend.service;
using SCLauncher.model.config;

namespace SCLauncher.ui.views.profiles;

public partial class ProfileSwitcher : UserControl
{
	private const string ManageProfilesItem = "Manage Profiles...";
	
	private readonly ProfilesService profilesService;

	public ProfileSwitcher()
	{
		InitializeComponent();
		profilesService = App.GetService<ProfilesService>();
		var backend = App.GetService<BackendService>();
		
		ProfileComboBox.SelectionChanged += OnProfileSelectionChanged;

		Refresh();
		profilesService.ProfileSwitched += (s, e) => Dispatcher.UIThread.Post(SelectActiveProfile);
		profilesService.ProfileAdded += (s, e) => Dispatcher.UIThread.Post(Refresh);
		profilesService.ProfileDeleted += (s, e) => Dispatcher.UIThread.Post(Refresh);
		backend.CanSwitchProfilesChanged += (s, val) => Dispatcher.UIThread.Post(() => ProfileComboBox.IsEnabled = val);
	}

	private void Refresh()
	{
		ProfileComboBox.Items.Clear();
		foreach (var profile in profilesService.Profiles)
		{
			ProfileComboBox.Items.Add(profile);
		}
		SelectActiveProfile();
		
		ProfileComboBox.Items.Add(ManageProfilesItem);
	}

	private void SelectActiveProfile()
	{
		var activeProfile = profilesService.ActiveProfile;
		foreach (var item in ProfileComboBox.Items)
		{
			if (item == activeProfile)
			{
				ProfileComboBox.SelectedItem = item;
				break;
			}
		}
	}

	private void OnProfileSelectionChanged(object? sender, SelectionChangedEventArgs args)
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
			profilesService.SetActiveProfile(selectedProfile.Name);
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
