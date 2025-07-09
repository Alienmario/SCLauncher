using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using SCLauncher.backend;
using SCLauncher.backend.install;
using SCLauncher.backend.util;
using SCLauncher.ui.controls;
using SCLauncher.ui.views;
using Application = Avalonia.Application;
using Notification = Avalonia.Controls.Notifications.Notification;

namespace SCLauncher.ui;

public partial class App : Application
{
	public new static App Current => (App)Application.Current!;
	
	private ServiceProvider? services;
	private WindowNotificationManager? notificationMgrBottom;

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
			
			notificationMgrBottom = new WindowNotificationManager(mainWindow)
			{
				Position = NotificationPosition.BottomCenter
			};
			
			#if !DEBUG
			CheckForUpdates();
			#endif
		}
		
		base.OnFrameworkInitializationCompleted();
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

	public static void ShowSuccess(string msg)
	{
		Current.notificationMgrBottom!.Show(new Notification(
			"Success", msg, NotificationType.Information, TimeSpan.FromSeconds(2))
		);
	}

	public static void ShowFailure(string msg)
	{
		Current.notificationMgrBottom!.Show(new Notification(
			"Failed", msg, NotificationType.Error, TimeSpan.FromSeconds(4))
		);
	}
	
	public static void ShowInfo(string msg)
	{
		Current.notificationMgrBottom!.Show(new Notification(
			"Info", msg, NotificationType.Information, TimeSpan.FromSeconds(4))
		);
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