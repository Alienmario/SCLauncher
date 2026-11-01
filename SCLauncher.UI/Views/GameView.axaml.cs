using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SCLauncher.Shared.Extensions;
using SCLauncher.Shared.Model.Config;
using SCLauncher.Shared.Service;

namespace SCLauncher.UI.Views;

public partial class GameView : UserControl
{
    private readonly IClientControlService clientController;
    private readonly IProfilesService profilesService;

    public GameView()
    {
        InitializeComponent();

        WindowModeCombo.ItemsSource = Enum.GetValues<ClientConfiguration.WindowModeEnum>().Select(e => e.GetDescription());

        profilesService = App.GetService<IProfilesService>();
        clientController = App.GetService<IClientControlService>();
        
        profilesService.ProfileSwitched += OnProfileSwitched;
        OnProfileSwitched(this, profilesService.ActiveProfile);
    }

    private void OnProfileSwitched(object? sender, AppProfile newProfile)
    {
        DataContext = newProfile.ClientConfig;
        LaunchButton.Content = "Launch " + newProfile.GameInstallFolder;
    }

    private void OnLaunchGameClicked(object? sender, RoutedEventArgs args)
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
        catch (Exception e)
        {
            App.ShowFailure(e.Message);
        }
    }
    
    private void OnConfiguratorResetClicked(object? sender, RoutedEventArgs args)
    {
        DataContext = profilesService.ActiveProfile.ResetClientConfig();
        ResetConfigButton?.Flyout?.Hide();
    }

}