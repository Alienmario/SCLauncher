using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SCLauncher.Shared.Extensions;

public static class ExceptionExtensions
{
	extension(Exception exception)
	{
		/// Logs the exception.
		public void Log()
		{
			Trace.WriteLine(exception);
		}

		/// Logs the exception with a custom message.
		public void Log(string message)
		{
			Trace.WriteLine(message);
			Trace.WriteLine(exception);
		}

		/// Extracts all nested messages from the exception chain.
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

		/// Finds the first nested exception (or self) of the specified type.
		public T? Find<T>(bool includeSelf = true) where T : Exception
		{
			if (includeSelf && exception is T typedException)
			{
				return typedException;
			}
			return exception.InnerException?.Find<T>();
		}

		/// Checks whether the exception chain (or self) contains an exception of the specified type.
		public bool Contains<T>(bool includeSelf = true) where T : Exception
		{
			return exception.Find<T>(includeSelf) != null;
		}
	}
}
