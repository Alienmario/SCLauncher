using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using SCLauncher.model;

namespace SCLauncher.backend.util;

public sealed class ConsoleMessageRewriter : IDisposable
{
	public BlockingCollection<StatusMessage> Messages { get; }
	
	private readonly MessageWriter outWriter;
	private readonly MessageWriter errorWriter;

	public ConsoleMessageRewriter(BlockingCollection<StatusMessage>? messages = null)
	{
		outWriter = new MessageWriter(this, MessageStatus.Info);
		errorWriter = new MessageWriter(this, MessageStatus.Error);
		Messages = messages ?? new BlockingCollection<StatusMessage>();
		
		Console.SetOut(outWriter);
		Console.SetError(errorWriter);
	}

	public void Dispose()
	{
		Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));
		Console.SetError(new StreamWriter(Console.OpenStandardError()));
		outWriter.Dispose();
		errorWriter.Dispose();
	}
	
	private sealed class MessageWriter(ConsoleMessageRewriter parent, MessageStatus status) : TextWriter
	{
		public override Encoding Encoding => Encoding.UTF8;

		public override void Write(string? value)
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				parent.Messages.Add(new StatusMessage(value, status));
			}
			base.Write(value);
		}

	}
}