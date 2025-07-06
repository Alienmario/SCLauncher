using System;

namespace SCLauncher.model.exception;

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