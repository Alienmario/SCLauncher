using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SCLauncher.backend.service;
using SCLauncher.model.config;
using SCLauncher.model.exception;

namespace SCLauncher.ui.views;

public partial class Game : UserControl
{
    private readonly ClientControlService clientController;
    private readonly ProfilesService profilesService;

    public Game()
    {
        InitializeComponent();

        WindowModeCombo.ItemsSource = Enum.GetValues<ClientConfiguration.WindowModeEnum>().Select(e => e.GetDescription());

        profilesService = App.GetService<ProfilesService>();
        clientController = App.GetService<ClientControlService>();
        
        profilesService.ProfileSwitched += OnProfileSwitched;
        OnProfileSwitched(this, profilesService.ActiveProfile);
    }

    private void OnProfileSwitched(object? sender, AppProfile newProfile)
    {
        DataContext = newProfile.ClientConfig;
        LaunchButton.Content = "Launch " + newProfile.GameInstallFolder;
    }

    private void OnLaunchGameClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (clientController.RunClient())
            {
                App.ShowSuccess("Launching game...");
            }
            else
            {
                App.ShowFailure("Unable to launch the game.");
            }
        }
        catch (InvalidGamePathException)
        {
            App.ShowFailure("Configured game path is invalid.");
        }
    }
    
    private void OnConfiguratorResetClicked(object? sender, RoutedEventArgs e)
    {
        DataContext = profilesService.ActiveProfile.NewClientConfig();
        ResetConfigButton?.Flyout?.Hide();
    }

}