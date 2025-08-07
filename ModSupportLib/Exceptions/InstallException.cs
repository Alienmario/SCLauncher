namespace ModSupportLib.Exceptions;

public class InstallException : Exception
{
	public InstallException(string? message) : base(message)
	{
	}

	public InstallException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}