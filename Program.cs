using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using SCLauncher.ui;
using Tmds.Utils;

namespace SCLauncher;

static class Program
{
	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
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
		
		TaskScheduler.UnobservedTaskException += (sender, e) =>
		{
			Trace.WriteLine(e.Exception);
			e.SetObserved();
		};

		return BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
	}

	// Extension method to log exceptions from tasks
	public static void LogExceptions(this Task task)
	{
		task.ContinueWith(t =>
		{
			var aggException = t.Exception!.Flatten();
			foreach (var exception in aggException.InnerExceptions)
			{
				Trace.WriteLine(exception);
			}
		},
		TaskContinuationOptions.OnlyOnFaulted);
	}
}