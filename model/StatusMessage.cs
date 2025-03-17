using System;

namespace SCLauncher.model;

public class StatusMessage(string text)
{
	public StatusMessage(string text, MessageStatus messageStatus) : this(text)
	{
		MessageStatus = messageStatus;
	}

	public string Text { get; } = text;

	public DateTime Time { get; init; } = DateTime.Now;

	public MessageStatus MessageStatus { get; init; } = MessageStatus.Info;

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