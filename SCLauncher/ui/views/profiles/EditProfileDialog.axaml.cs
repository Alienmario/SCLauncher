using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SCLauncher.backend.service;
using SCLauncher.model.config;
using SCLauncher.ui.controls;

namespace SCLauncher.ui.views.profiles;

public partial class EditProfileDialog : BaseDialogWindow
{
	private readonly BackendService backendService;

	public EditProfileDialog()
	{
		InitializeComponent();
		backendService = App.GetService<BackendService>();

		DataContextChanged += (sender, args) =>
		{
			if (DataContext is AppProfile profile)
			{
				LoadProfile(profile);
			}
		};
		if (Design.IsDesignMode)
		{
			DataContext = AppProfile.CreateDefaultBlackMesa();
		}

		CancelButton.Click += OnCancelClick;
		SaveButton.Click += OnSaveClick;
	}

	private void LoadProfile(AppProfile profile)
	{
		AppTypeLabel.Content = profile.AppType.GetDescription();
		ProfileNameTextBox.Text = profile.Name;
		GameAppIdTextBox.Text = profile.GameAppId.ToString();
		ServerAppIdTextBox.Text = profile.ServerAppId.ToString();
		GameInstallFolderTextBox.Text = profile.GameInstallFolder;
		ServerInstallFolderTextBox.Text = profile.ServerInstallFolder;
		ModFolderTextBox.Text = profile.ModFolder;

		GameExecutableWinTextBox.Text = profile.GameExecutable
			.TryGetValue(PlatformID.Win32NT, out var winExe) ? winExe : string.Empty;
		GameExecutableLinuxTextBox.Text = profile.GameExecutable
			.TryGetValue(PlatformID.Unix, out var linuxExe) ? linuxExe : string.Empty;

		GamePathTextBox.Text = profile.GamePath ?? string.Empty;
		ServerPathTextBox.Text = profile.ServerPath ?? string.Empty;
	}

	private bool SaveProfile(AppProfile profile)
	{
		var name = ProfileNameTextBox.Text?.Trim();

		if (string.IsNullOrWhiteSpace(name))
		{
			ShowError("Please enter a profile name");
			return false;
		}

		// Check if name is different and if it conflicts with another profile
		if (!name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase) &&
		    backendService.Profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
		{
			ShowError("A profile with this name already exists");
			return false;
		}

		// Validate Game App ID
		if (!uint.TryParse(GameAppIdTextBox.Text?.Trim(), out var gameAppId))
		{
			ShowError("Please enter a valid Game App ID");
			return false;
		}

		// Validate Server App ID
		if (!uint.TryParse(ServerAppIdTextBox.Text?.Trim(), out var serverAppId))
		{
			ShowError("Please enter a valid Server App ID");
			return false;
		}

		// Validate required string fields
		var gameInstallFolder = GameInstallFolderTextBox.Text?.Trim();
		if (string.IsNullOrWhiteSpace(gameInstallFolder))
		{
			ShowError("Please enter a game install folder");
			return false;
		}

		var serverInstallFolder = ServerInstallFolderTextBox.Text?.Trim();
		if (string.IsNullOrWhiteSpace(serverInstallFolder))
		{
			ShowError("Please enter a server install folder");
			return false;
		}

		var modFolder = ModFolderTextBox.Text?.Trim();
		if (string.IsNullOrWhiteSpace(modFolder))
		{
			ShowError("Please enter a mod folder");
			return false;
		}

		var gameExecutableWin = GameExecutableWinTextBox.Text?.Trim();
		if (string.IsNullOrWhiteSpace(gameExecutableWin))
		{
			ShowError("Please enter a Windows game executable");
			return false;
		}

		var gameExecutableLinux = GameExecutableLinuxTextBox.Text?.Trim();
		if (string.IsNullOrWhiteSpace(gameExecutableLinux))
		{
			ShowError("Please enter a Linux game executable");
			return false;
		}

		try
		{
			profile.Name = name;
			profile.GameAppId = gameAppId;
			profile.ServerAppId = serverAppId;
			profile.GameInstallFolder = gameInstallFolder;
			profile.ServerInstallFolder = serverInstallFolder;
			profile.ModFolder = modFolder;
			profile.GameExecutable = new Dictionary<PlatformID, string>
			{
				{ PlatformID.Win32NT, gameExecutableWin },
				{ PlatformID.Unix, gameExecutableLinux }
			};
			profile.GamePath = GamePathTextBox.Text?.Trim();
			profile.ServerPath = ServerPathTextBox.Text?.Trim();

			return true;
		}
		catch (Exception ex)
		{
			ShowError($"Failed to update profile: {ex.Message}");
			return false;
		}
	}
	
	private void OnCancelClick(object? sender, RoutedEventArgs e)
	{
		Close(null);
	}

	private void OnSaveClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is AppProfile profile)
		{
			if (SaveProfile(profile))
			{
				Close(profile);
			}
		}
	}

	private void ShowError(string message)
	{
		ErrorMessage.Text = message;
		ErrorMessage.IsVisible = true;
	}
}
