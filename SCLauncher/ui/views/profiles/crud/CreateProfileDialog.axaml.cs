using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SCLauncher.backend.service;
using SCLauncher.model.definition;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.profiles.crud;

public partial class CreateProfileDialog : BaseDialogWindow
{
	private readonly ProfilesService profilesService;

	public CreateProfileDialog()
	{
		InitializeComponent();
		profilesService = App.GetService<ProfilesService>();
		
		CancelButton.Click += OnCancelClick;
		CreateButton.Click += OnCreateClick;
		
		AppTypeComboBox.ItemsSource = Enum.GetValues<AppType>();
		AppTypeComboBox.SelectionChanged += OnAppTypeChanged;
		
		Activated += delegate
		{
			AppTypeComboBox.Focus();
		};
	}

	private void OnAppTypeChanged(object? sender, SelectionChangedEventArgs e)
	{
		// Populate presets
		AppType appType = (AppType)AppTypeComboBox.SelectedItem!;
		AppPresetComboBox.ItemsSource = AppDefinitions.Get(appType).AvailablePresets;
		AppPresetComboBox.SelectedIndex = 0;
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e)
	{
		Close(null);
	}

	private void OnCreateClick(object? sender, RoutedEventArgs args)
	{
		string? name = ProfileNameTextBox.Text?.Trim();
		AppType appType = (AppType)AppTypeComboBox.SelectedItem!;
		AppPreset appPreset = (AppPreset)AppPresetComboBox.SelectedItem!;

		if (string.IsNullOrWhiteSpace(name))
		{
			ShowError("Please enter a profile name");
			return;
		}

		if (profilesService.Profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
		{
			ShowError("A profile with this name already exists");
			return;
		}

		try
		{
			var newProfile = profilesService.CreateProfile(appType, appPreset, name);
			Close(newProfile);
		}
		catch (Exception e)
		{
			ShowError($"Failed to create profile: {e.Message}");
			e.Log();
		}
	}

	private void ShowError(string message)
	{
		ErrorMessage.Text = message;
		ErrorMessage.IsVisible = true;
	}
}
