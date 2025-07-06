using System;

namespace SCLauncher.model.exception;

public class InvalidSteamPathException : Exception
{
	public InvalidSteamPathException()
	{
	}

	public InvalidSteamPathException(string? message) : base(message)
	{
	}

	public InvalidSteamPathException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}