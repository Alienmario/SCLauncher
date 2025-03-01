using Microsoft.Extensions.DependencyInjection;
using SCLauncher.backend.service;
using SCLauncher.model;
using SCLauncher.ui.views;

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
	}
	
}