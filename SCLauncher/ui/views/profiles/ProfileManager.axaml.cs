using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SCLauncher.backend.service;
using SCLauncher.model.config;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.profiles;

internal record ProfileManagerEntry(AppProfile Profile, bool IsActive)
{
	public virtual bool Equals(ProfileManagerEntry? other)
	{
		return other is not null && Profile.Name == other.Profile.Name;
	}

	public override int GetHashCode()
	{
		return Profile.Name.GetHashCode();
	}
}

public partial class ProfileManager : BaseDialogWindow
{
	private readonly BackendService backendService;

	public ProfileManager()
	{
		InitializeComponent();
		backendService = App.GetService<BackendService>();
		backendService.ProfileSwitched += (s, e) => { LoadProfiles(); };

		AddButton.Click += OnAddClick;
		EditButton.Click += OnEditClick;
		DeleteButton.Click += OnDeleteClick;
		CloseButton.Click += OnCloseClick;
		ProfilesListBox.SelectionChanged += OnSelectionChanged;

		LoadProfiles();
	}

	private void LoadProfiles()
	{
		ProfilesListBox.ItemsSource = backendService.Profiles
			.Select(p => new ProfileManagerEntry(p, p == backendService.ActiveProfile));
	}

	private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		var hasSelection = ProfilesListBox.SelectedItem != null;
		EditButton.IsEnabled = hasSelection;
		DeleteButton.IsEnabled = hasSelection;
	}

	private async void OnAddClick(object? sender, RoutedEventArgs args)
	{
		try
		{
			var dialog = new CreateProfileDialog();
			var result = await dialog.ShowDialog<AppProfile?>(this);

			if (result != null)
			{
				LoadProfiles();
				ProfilesListBox.SelectedIndex = ProfilesListBox.Items.Count - 1;
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private void OnEditClick(object? sender, RoutedEventArgs args)
	{
		if (ProfilesListBox.SelectedItem is ProfileManagerEntry selectedEntry)
		{
			EditProfile(selectedEntry.Profile);
		}
	}

	private void OnDoubleTapped(object? sender, TappedEventArgs e)
	{
		if (ProfilesListBox.SelectedItem is ProfileManagerEntry entry)
		{
			EditProfile(entry.Profile);
		}
	}

	private async void OnDeleteClick(object? sender, RoutedEventArgs args)
	{
		try
		{
			if (ProfilesListBox.SelectedItem is ProfileManagerEntry selectedEntry)
			{
				if (backendService.Profiles.Count <= 1)
				{
					App.ShowFailure("Cannot delete the last profile", this);
					return;
				}

				var confirmDialog = new DeleteProfileDialog
				{
					DataContext = selectedEntry.Profile
				};
				var confirmed = await confirmDialog.ShowDialog<bool>(this);

				if (confirmed)
				{
					if (backendService.DeleteProfile(selectedEntry.Profile.Name))
					{
						LoadProfiles();
					}
					else
					{
						App.ShowFailure("Failed to delete profile", this);
					}
				}
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private void OnCloseClick(object? sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void EditProfile(AppProfile profile)
	{
		try
		{
			var dialog = new EditProfileDialog
			{
				DataContext = profile
			};
			var result = await dialog.ShowDialog<AppProfile?>(this);

			if (result != null)
			{
				LoadProfiles();
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	private void OnActivateClick(object? sender, RoutedEventArgs args)
	{
		if (sender is Control { DataContext: ProfileManagerEntry entry })
		{
			if (backendService.SetActiveProfile(entry.Profile.Name))
			{
				ProfilesListBox.SelectedItem = entry;
			}
		}
	}
}
