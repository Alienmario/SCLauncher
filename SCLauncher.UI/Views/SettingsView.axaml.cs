using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SCLauncher.Shared.Model.Config;
using SCLauncher.Shared.Service;
using SCLauncher.UI.Views.Settings;

namespace SCLauncher.UI.Views;

public partial class SettingsView : UserControl
{
	private readonly IPersistenceService _persistenceService;
	
	public SettingsView()
	{
		InitializeComponent();

		_persistenceService = App.GetService<IPersistenceService>();
		var profilesService = App.GetService<IProfilesService>();
		GlobalConfigContent.DataContext = App.GetService<GlobalConfiguration>();
		
		profilesService.ProfileSwitched += OnProfileSwitched;
		OnProfileSwitched(this, profilesService.ActiveProfile);
	}

	private void OnProfileSwitched(object? sender, AppProfile newProfile)
	{
		DataContext = newProfile;
	}

	private async void OnBrowseLocalDataClicked(object? sender, RoutedEventArgs args)
	{
		try
		{
			var dir = _persistenceService.DataDirectory;
			if (dir != null && !await TopLevel.GetTopLevel(this)!.Launcher
				    .LaunchDirectoryInfoAsync(new DirectoryInfo(dir)))
			{
				App.ShowFailure("Unable to open local data directory.");
			}
		}
		catch (Exception ex)
		{
			ex.Log();
		}
	}

	private async void OnEraseAllDataClicked(object? sender, RoutedEventArgs args)
	{
		var confirmDialog = new EraseDataDialog();
		if (await confirmDialog.ShowDialog<bool>(App.MainWindow))
		{
			try
			{
				_persistenceService.EraseAll();
			}
			catch (Exception e)
			{
				e.Log();
				App.ShowFailure("Failed to erase local data.");
				return;
			}
			try
			{
				Process.Start(Environment.ProcessPath!);
				Environment.Exit(0);
			}
			catch (Exception e)
			{
				e.Log();
				App.ShowFailure("Failed to restart the launcher.");
			}
		}
	}

}