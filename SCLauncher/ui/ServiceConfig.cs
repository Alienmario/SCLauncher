using Microsoft.Extensions.DependencyInjection;
using SCLauncher.ui.views;

namespace SCLauncher.ui;

public static class ServiceConfig
{
	// The following code is creating an extension method for IServiceCollection
	// that will register services to our service collection and make them available for injection.
	public static void AddUIServices(this IServiceCollection collection)
	{
		collection.AddSingleton<MainWindow>();
	}
}