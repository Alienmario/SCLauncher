using Avalonia.Controls;
using Avalonia.Interactivity;
using SCLauncher.backend.service;

namespace SCLauncher.ui.views;

public partial class Game : UserControl
{
    private readonly ClientControlService clientController;
    private readonly BackendService backend;

    public Game()
    {
        InitializeComponent();

        backend = App.GetService<BackendService>();
        clientController = App.GetService<ClientControlService>();
        DataContext = backend.GetClientConfig();
    }

    private void OnLaunchGameClicked(object? sender, RoutedEventArgs e)
    {
        clientController.RunClient();
    }
    
    private void OnConfiguratorResetClicked(object? sender, RoutedEventArgs e)
    {
        DataContext = backend.GetClientConfig(true);
        ResetConfigButton?.Flyout?.Hide();
    }

}