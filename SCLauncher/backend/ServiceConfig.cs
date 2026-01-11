using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using SCLauncher.backend.install;
using SCLauncher.backend.serverinstall;
using SCLauncher.backend.serverinstall.components;
using SCLauncher.backend.serverinstall.components.plugins;
using SCLauncher.backend.service;
using SCLauncher.model.config;
using SCLauncher.model.install;

namespace SCLauncher.backend;

public static class ServiceConfig
{
	// The following code is creating an extension method for IServiceCollection
	// that will register services to our service collection and make them available for injection.
	public static void AddBackendServices(this IServiceCollection collection)
	{
		// Configs
		collection.AddSingleton<GlobalConfiguration>();
		
		// Services
		collection.AddSingleton<BackendService>();
		collection.AddSingleton<ProfilesService>();
		collection.AddSingleton<ServerInstallService>();
		collection.AddSingleton<ServerControlService>();
		collection.AddSingleton<ServerMessageAnalyzerService>();
		collection.AddSingleton<ClientControlService>();
		collection.AddSingleton<PersistenceService>();
		collection.AddSingleton<ServerBrowserService>();

		// Server install
		collection.AddSingleton<ServerInstallRunner>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, DedicatedServerInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, SrcdsFixInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, MetaModInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, SourceModInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, SourceCoopInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, ModelChooserInstaller>();

		collection.AddSingleton(new HttpClient());
		collection.AddSingleton<InstallHelper>();
	}
	
}