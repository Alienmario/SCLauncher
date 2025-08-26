namespace ModSupportLib.Exceptions;

public class RepositoryLoadException : Exception
{
	public RepositoryLoadException(string? message) : base(message)
	{
	}

	public RepositoryLoadException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}