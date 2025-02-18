using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SCLauncher.backend;
using SCLauncher.backend.service;
using SCLauncher.ui.views;

namespace SCLauncher.ui;

public partial class App : Application
{
	private ServiceProvider? _services;

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		// Register all the services needed for the application to run
		var services = new ServiceCollection();
		services.AddBackendServices();
		services.AddUIServices();
		_services = services.BuildServiceProvider();

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			MainWindow mainWindow = GetService<MainWindow>();
			desktop.MainWindow = mainWindow;
		}
		
		GetService<BackendService>().Initialize();

		base.OnFrameworkInitializationCompleted();
	}

	public static T GetService<T>() where T : class
	{
		return (Current as App)!._services!.GetRequiredService<T>();
	}
}