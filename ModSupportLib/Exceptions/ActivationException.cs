namespace ModSupportLib.Exceptions;

public class ActivationException : Exception
{
	public ActivationException(string? message) : base(message)
	{
	}

	public ActivationException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}