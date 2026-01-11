using System;
using System.Linq;
using Avalonia.Interactivity;
using SCLauncher.backend.service;
using SCLauncher.model.config;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.profiles;

public partial class CreateProfileDialog : BaseDialogWindow
{
	private readonly ProfilesService profilesService;

	public CreateProfileDialog()
	{
		InitializeComponent();
		profilesService = App.GetService<ProfilesService>();
		CancelButton.Click += OnCancelClick;
		CreateButton.Click += OnCreateClick;
		AppTypeComboBox.ItemsSource = Enum.GetValues<AppType>().Select(e => e.GetDescription());
		
		Activated += delegate
		{
			ProfileNameTextBox.Focus();
			InvalidateArrange(); // SizeToContent bugfix
		};
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e)
	{
		Close(null);
	}

	private void OnCreateClick(object? sender, RoutedEventArgs e)
	{
		string? name = ProfileNameTextBox.Text?.Trim();
		AppType appType = (AppType)AppTypeComboBox.SelectedIndex;

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
			var newProfile = profilesService.CreateProfile(appType, name);
			Close(newProfile);
		}
		catch (Exception ex)
		{
			ShowError($"Failed to create profile: {ex.Message}");
		}
	}

	private void ShowError(string message)
	{
		ErrorMessage.Text = message;
		ErrorMessage.IsVisible = true;
	}
}
