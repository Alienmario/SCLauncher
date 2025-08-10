namespace ModSupportLib.Exceptions;

public class AlreadyInstalledException : InstallException
{
	public AlreadyInstalledException(string? message) : base(message)
	{
	}

	public AlreadyInstalledException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}