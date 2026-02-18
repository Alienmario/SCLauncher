using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using SCLauncher.backend;
using SCLauncher.backend.install;
using SCLauncher.backend.service;
using SCLauncher.backend.util;
using SCLauncher.model.config;
using SCLauncher.ui.controls;
using SCLauncher.ui.views;
using SCLauncher.ui.views.profiles;
using Application = Avalonia.Application;
using Notification = Avalonia.Controls.Notifications.Notification;

namespace SCLauncher.ui;

public partial class App : Application
{
	public new static App Current => (App)Application.Current!;
	
	private ServiceProvider? services;
	private readonly Dictionary<Window, WindowNotificationManager> notificationManagers = new();

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		// Register all the services needed for the application to run
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddBackendServices();
		serviceCollection.AddUIServices();
		services = serviceCollection.BuildServiceProvider();
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

			bool checkForUpdates = GetService<GlobalConfiguration>().CheckForUpdates;
#if !DEBUG
			if (checkForUpdates) CheckForUpdates();
#endif
		}
	}

	public static T GetService<T>() where T : class
	{
		return Current.services!.GetRequiredService<T>();
	}
	
	public static object? GetResource(string name)
	{
		Current.TryGetResource(name, Current.ActualThemeVariant, out object? res);
		return res;
	}

	private WindowNotificationManager GetNotificationManager(Window? window)
	{
		// If no window is provided, try to get the MainWindow
		if (window == null)
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				window = desktop.MainWindow;
			}

			if (window == null)
			{
				throw new InvalidOperationException("No window available for notifications");
			}
		}

		// Check if we already have a notification manager for this window
		if (notificationManagers.TryGetValue(window, out var existingManager))
		{
			return existingManager;
		}

		// Create a new notification manager for this window
		var newManager = new WindowNotificationManager(window)
		{
			Position = NotificationPosition.BottomCenter
		};

		notificationManagers[window] = newManager;

		// Subscribe to the window's Closed event to clean up the dictionary
		window.Closed += (sender, e) =>
		{
			if (sender is Window closedWindow)
			{
				notificationManagers.Remove(closedWindow);
			}
		};

		return newManager;
	}

	public static void ShowSuccess(string msg, Window? window = null)
	{
		var manager = Current.GetNotificationManager(window);
		manager.Show(new Notification("Success", msg, NotificationType.Information, TimeSpan.FromSeconds(2)));
	}

	public static void ShowFailure(string msg, Window? window = null)
	{
		var manager = Current.GetNotificationManager(window);
		manager.Show(new Notification("Failed", msg, NotificationType.Error, TimeSpan.FromSeconds(4)));
	}
	
	public static void ShowInfo(string msg, Window? window = null)
	{
		var manager = Current.GetNotificationManager(window);
		manager.Show(new Notification("Info", msg, NotificationType.Information, TimeSpan.FromSeconds(4)));
	}
	
	public static string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString(2) ?? string.Empty;

	public static void CheckForUpdates()
	{
		Task.Run(async () =>
		{
			GitHubClient github = GetService<InstallHelper>().GithubClient;
			var latestRelease = await github.Repository.Release.GetLatest("Alienmario", "SCLauncher");
			string latestVersion = latestRelease.TagName;
			string localVersion = Version;
			
			Trace.WriteLine($"Checking SCLauncher version [Current: {localVersion}, Latest: {latestVersion}]");
			if (VersionUtils.SmartCompare(localVersion, latestVersion) < 0)
			{
				Dispatcher.UIThread.Post(() =>
				{
					var notification = GetService<MainWindow>().FindLogicalDescendantOfType<UpdateNotification>();
					if (notification != null)
					{
						notification.Url = latestRelease.HtmlUrl;
						notification.Show();
					}
				});
			}
		}).LogExceptions();
	}
	
}