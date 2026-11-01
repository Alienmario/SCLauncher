using System;

namespace SCLauncher.Shared.Exceptions;

public class InvalidServerPathException : Exception
{
	public InvalidServerPathException()
	{
	}

	public InvalidServerPathException(string? message) : base(message)
	{
	}

	public InvalidServerPathException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}