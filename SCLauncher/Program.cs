using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using SCLauncher.ui;
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

	/// Extension method to log exceptions from tasks.
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

	extension(Exception exception)
	{
		/// Extension method that logs an exception.
		public void Log()
		{
			Trace.WriteLine(exception);
		}

		public void Log(string message)
		{
			Trace.WriteLine(message);
			Trace.WriteLine(exception);
		}

		/// Extension method for extracting all nested messages from exceptions.
		public string GetAllMessages(string delimiter = " - ")
		{
			var messages = new List<string>();
			for (;;)
			{
				if (!string.IsNullOrWhiteSpace(exception.Message))
					messages.Add(exception.Message);
				if (exception.InnerException == null)
					break;
				exception = exception.InnerException;
			}
			return string.Join(delimiter, messages);
		}

		/// Scanner extension that finds first nested exception (or self) of specific type.
		public T? Find<T>(bool includeSelf = true) where T : Exception
		{
			if (includeSelf && exception is T typedException)
			{
				return typedException;
			}
			return exception.InnerException?.Find<T>();
		}

		public bool Contains<T>(bool includeSelf = true) where T : Exception
		{
			return exception.Find<T>(includeSelf) != null;
		}
	}

	/// Extension method for extracting an enum value's description (DescriptionAttribute).
	/// Fallbacks to definition string.
	public static string GetDescription(this Enum enumValue)
	{
		var field = enumValue.GetType().GetField(enumValue.ToString());
		if (field == null)
			return enumValue.ToString();

		if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
		{
			return attribute.Description;
		}

		return enumValue.ToString();
	}
	
}