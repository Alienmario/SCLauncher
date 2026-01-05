using System;
using System.Linq;
using Avalonia.Interactivity;
using SCLauncher.backend.service;
using SCLauncher.model.config;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.profiles;

public partial class CreateProfileDialog : BaseDialogWindow
{
	private readonly BackendService backendService;

	public CreateProfileDialog()
	{
		InitializeComponent();
		backendService = App.GetService<BackendService>();
		
		AppTypeComboBox.ItemsSource = Enum.GetValues<AppType>().Select(e => e.GetDescription());

		CancelButton.Click += OnCancelClick;
		CreateButton.Click += OnCreateClick;
		Activated += delegate { ProfileNameTextBox.Focus(); };
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

		if (backendService.Profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
		{
			ShowError("A profile with this name already exists");
			return;
		}

		try
		{
			var newProfile = backendService.CreateProfile(appType, name);
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
