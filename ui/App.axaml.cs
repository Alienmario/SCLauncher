using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SCLauncher.backend;
using SCLauncher.ui.views;

namespace SCLauncher.ui;

public partial class App : Application
{
	private ServiceProvider? services;

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
		this.services = services.BuildServiceProvider();

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			MainWindow mainWindow = GetService<MainWindow>();
			desktop.MainWindow = mainWindow;
		}
		
		base.OnFrameworkInitializationCompleted();
	}

	public static T GetService<T>() where T : class
	{
		return (Current as App)!.services!.GetRequiredService<T>();
	}
	
	public static object? GetResource(string name)
	{
		Current!.TryGetResource(name, Current.ActualThemeVariant, out object? res);
		return res;
	}

	public static string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString(2) ?? string.Empty;
	
}