using System;

namespace SCLauncher.model.exception;

public class ApiRateLimitException : Exception
{
	public DateTimeOffset? NextAttempt { get; init; }

	public ApiRateLimitException()
	{
	}

	public ApiRateLimitException(DateTimeOffset nextAttempt, Exception? innerException = null)
		: base(null, innerException)
	{
		NextAttempt = nextAttempt;
	}

	public ApiRateLimitException(TimeSpan retryAfter, Exception? innerException = null)
		: base(null, innerException)
	{
		NextAttempt = DateTimeOffset.Now + retryAfter;
	}

	public override string Message
	{
		get
		{
			string? retry = null;
			if (NextAttempt != null)
			{
				retry = $". Try again after {NextAttempt.Value.LocalDateTime:t}.";
			}
			return $"API Rate Limit exceeded{retry}";
		}
	}
}
