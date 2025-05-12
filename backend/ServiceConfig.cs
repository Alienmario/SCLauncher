using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using SCLauncher.backend.install;
using SCLauncher.backend.serverinstall;
using SCLauncher.backend.serverinstall.components;
using SCLauncher.backend.service;
using SCLauncher.model;
using SCLauncher.model.install;

namespace SCLauncher.backend;

public static class ServiceConfig
{
	// The following code is creating an extension method for IServiceCollection
	// that will register services to our service collection and make them available for injection.
	public static void AddBackendServices(this IServiceCollection collection)
	{
		collection.AddSingleton<ConfigHolder>();
		collection.AddSingleton<BackendService>();
		collection.AddSingleton<ServerInstallService>();
		collection.AddSingleton<PersistenceService>();
		
		// Server install
		collection.AddSingleton<ServerInstallRunner>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, DedicatedServerInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, MetaModInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, SourceModInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, SourceCoopInstaller>();
		collection.AddSingleton<IServerComponentInstaller<ComponentInfo>, SrcdsFixInstaller>();
		
		collection.AddSingleton(new HttpClient());
		collection.AddSingleton<InstallHelper>();
	}
	
}