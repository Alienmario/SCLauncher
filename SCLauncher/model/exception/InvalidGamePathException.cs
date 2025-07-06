using System;

namespace SCLauncher.model.exception;

public class InvalidGamePathException : Exception
{
	public InvalidGamePathException()
	{
	}

	public InvalidGamePathException(string? message) : base(message)
	{
	}

	public InvalidGamePathException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}