using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Markdig;
using MarkView.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using SCLauncher.backend;
using SCLauncher.backend.service;
using SCLauncher.model.config;
using SCLauncher.ui.views;
using SCLauncher.ui.views.profiles.init;

namespace SCLauncher.ui;

public class App : Application
{
	public new static App Current => (App)Application.Current!;

	public static MainWindow MainWindow => GetService<MainWindow>();
	
	public static readonly string Version = Assembly.GetExecutingAssembly()
		.GetName().Version?.ToString(2) ?? string.Empty;

	public static readonly string? RepositoryUrl = Assembly.GetExecutingAssembly()
		.GetCustomAttributes<AssemblyMetadataAttribute>()
		.FirstOrDefault(a => a.Key.Equals("RepositoryUrl", StringComparison.OrdinalIgnoreCase))?.Value;

	private ServiceProvider? _services;
	private readonly ConditionalWeakTable<TopLevel, WindowNotificationManager> _notificationManagers = new();

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		Updater.CleanupExecutableBackup();
		
		MarkdownViewerDefaults.Pipeline = new MarkdownPipelineBuilder()
			.UseSupportedExtensions()
			.Build();
			
		// Register all the services needed for the application to run
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddBackendServices();
		serviceCollection.AddUIServices();
		_services = serviceCollection.BuildServiceProvider();
		GetService<BackendService>().Initialize();

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			if (GetService<ProfilesService>().Profiles.Count == 0)
			{
				desktop.MainWindow = new InitializeProfilesDialog { ConfirmHandler = OnProfilesInitialized };
			}
			else
			{
				OnProfilesInitialized();
			}
		}
		
		base.OnFrameworkInitializationCompleted();
	}

	private void OnProfilesInitialized()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			MainWindow mainWindow = GetService<MainWindow>();
			mainWindow.Show();
			mainWindow.Focus();
			desktop.MainWindow = mainWindow;

			// Cache the MainWindow's notification manager
			GetNotificationManager(mainWindow);
			
			var config = GetService<GlobalConfiguration>();
#if !DEBUG
			if (config.CheckForUpdates)
			{
				CheckForUpdates();
			}
			
			config.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == nameof(GlobalConfiguration.CheckForUpdates) && config.CheckForUpdates)
				{
					Dispatcher.UIThread.Post(CheckForUpdates);
				}
			};
#endif
		}
	}

	public static T GetService<T>() where T : class
	{
		return Current._services!.GetRequiredService<T>();
	}
	
	public static object? GetResource(string name)
	{
		Current.TryGetResource(name, Current.ActualThemeVariant, out object? res);
		return res;
	}

	public WindowNotificationManager GetNotificationManager(TopLevel? topLevel)
	{
		// If no topLevel is provided, try to get the current main window
		if (topLevel == null)
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				topLevel = desktop.MainWindow;
			}

			if (topLevel == null)
			{
				throw new InvalidOperationException("No topLevel available for notifications");
			}
		}

		// Special handling for popups with custom placement
		foreach (Popup popup in topLevel.OpenedPopups.Reverse())
		{
			var popupManager = popup.FindLogicalDescendantOfType<WindowNotificationManager>();
			if (popupManager != null)
				return popupManager;
		}

		// Check if we already have or add a notification manager for this topLevel
		return _notificationManagers.GetOrAdd(topLevel, tl => new WindowNotificationManager(tl)
		{
			Position = NotificationPosition.BottomCenter
		});
	}

	public static void ShowSuccess(string msg, TopLevel? topLevel = null)
	{
		var manager = Current.GetNotificationManager(topLevel);
		manager.Show(new Notification("Success", msg, NotificationType.Information, TimeSpan.FromSeconds(2)));
	}

	public static void ShowFailure(string msg, TopLevel? topLevel = null)
	{
		var manager = Current.GetNotificationManager(topLevel);
		manager.Show(new Notification("Failed", msg, NotificationType.Error, TimeSpan.FromSeconds(4)));
	}
	
	public static void ShowInfo(string msg, TopLevel? topLevel = null)
	{
		var manager = Current.GetNotificationManager(topLevel);
		manager.Show(new Notification("Info", msg, NotificationType.Information, TimeSpan.FromSeconds(4)));
	}
	
	public static async void CheckForUpdates()
	{
		try
		{
			var releases = await GetService<Updater>().CheckForUpdates();
			if (releases.Count > 0)
			{
				MainWindow.UpdateNotification.Show(releases);
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}
	
}