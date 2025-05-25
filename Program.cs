using System;
using System.Collections.Generic;
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
		
		Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
		TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
		{
			eventArgs.Exception.Log();
			eventArgs.SetObserved();
		};

		return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}

	// Extension method to log exceptions from tasks.
	public static void LogExceptions(this Task task)
	{
		task.ContinueWith(t =>
		{
			var aggException = t.Exception!.Flatten();
			foreach (var exception in aggException.InnerExceptions)
			{
				exception.Log();
			}
		},
		TaskContinuationOptions.OnlyOnFaulted);
	}

	// Extension method that logs an exception.
	public static void Log(this Exception exception)
	{
		Trace.WriteLine(exception);
	}

	// Extension method for extracting all nested messages from exceptions.
	public static string GetAllMessages(this Exception? e, string delimiter = " - ")
	{
		var messages = new List<string>();
		do
		{
			if (!string.IsNullOrWhiteSpace(e!.Message))
				messages.Add(e.Message);
			e = e.InnerException;
		}
		while (e != null);
		return string.Join(delimiter, messages);
	}
}