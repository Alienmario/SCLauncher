namespace ModSupportLib.Exceptions;

public class UninstallException : Exception
{
	public UninstallException(string? message) : base(message)
	{
	}

	public UninstallException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}