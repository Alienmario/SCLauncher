using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using SCLauncher.Shared.Extensions;
using SCLauncher.UI;
using Tmds.Utils;

namespace SCLauncher;

static class Program
{
	// Avalonia configuration, don't remove; also used by visual designer.
	#pragma warning disable AVALONIA_X11_CSD
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.With(new X11PlatformOptions
			{
				// Let Avalonia draw the decorations, so windows opting in via
				// ExtendClientAreaToDecorationsHint get a custom titlebar instead of the WM one.
				EnableDrawnDecorations = true
			})
			.WithInterFont()
			.LogToTrace();

	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static int Main(string[] args)
	{
		// Starting a subprocess function? [Tmds.ExecFunction]
		if (ExecFunction.IsExecFunctionCommand(args))
		{
			return ExecFunction.Program.Main(args);
		}
		
		Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
		
		TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
		{
			eventArgs.Exception.Log();
			eventArgs.SetObserved();
		};

		// Started headless by the updater?
		if (Updater.IsFinishUpdateCommand(args))
		{
			return Updater.FinishUpdate(args);
		}
		
		Updater.Cleanup();
		return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}
}
