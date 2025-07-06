using System;

namespace SCLauncher.model;

public class StatusMessage(string text)
{
	public StatusMessage(string text, MessageStatus status) : this(text)
	{
		Status = status;
	}

	public string Text { get; } = text;

	public DateTime Time { get; init; } = DateTime.Now;

	public MessageStatus Status { get; init; } = MessageStatus.Info;

	public string? Details { get; set; }

	public override string ToString()
	{
		return Text;
	}
}

public enum MessageStatus
{
	Info,
	Success,
	Warning,
	Error
}