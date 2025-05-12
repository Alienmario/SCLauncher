using System;

namespace SCLauncher.backend.install;

public class InstallException : Exception
{
	public InstallException()
	{
	}

	public InstallException(string? message) : base(message)
	{
	}

	public InstallException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}