using System.Threading.Tasks;

namespace SCLauncher.Shared.Extensions;

public static class TaskExtensions
{
	/// Logs any exceptions thrown by the task.
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
}
