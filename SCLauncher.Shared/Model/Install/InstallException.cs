using System;

namespace SCLauncher.Shared.Model.Install;

public class InstallException : Exception
{
	public InstallException() : base("")
	{
	}

	public InstallException(string? message) : base(message)
	{
	}

	public InstallException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}