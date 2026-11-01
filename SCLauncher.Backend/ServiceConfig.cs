using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using SCLauncher.Backend.Install;
using SCLauncher.Backend.ServerInstall;
using SCLauncher.Backend.ServerInstall.Components;
using SCLauncher.Backend.ServerInstall.Components.Plugins;
using SCLauncher.Backend.Service;
using SCLauncher.Shared.Model.Config;
using SCLauncher.Shared.Model.Install;
using SCLauncher.Shared.Service;

namespace SCLauncher.Backend;

public static class ServiceConfig
{
	// The following code is creating an extension method for IServiceCollection
	// that will register services to our service collection and make them available for injection.
	public static void AddBackendServices(this IServiceCollection collection)
	{
		// Configs
		collection.AddSingleton<GlobalConfiguration>();
		
		// Services
		collection.AddSingleton<IBackendService, BackendService>();
		collection.AddSingleton<IProfilesService, ProfilesService>();
		collection.AddSingleton<IServerInstallService, ServerInstallService>();
		collection.AddSingleton<IServerControlService, ServerControlService>();
		collection.AddSingleton<IServerMessageAnalyzerService, ServerMessageAnalyzerService>();
		collection.AddSingleton<IClientControlService, ClientControlService>();
		collection.AddSingleton<IPersistenceService, PersistenceService>();
		collection.AddSingleton<IServerBrowserService, ServerBrowserService>();
		collection.AddSingleton<IInstallHelperService, InstallHelperService>();

		// Server install
		collection.AddSingleton<ServerInstallRunner>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, DedicatedServerInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, SrcdsFixInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, MetaModInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, SourceModInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, SourceCoopInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, ModelChooserInstaller>();

		collection.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
	}
	
}