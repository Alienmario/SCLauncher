using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

namespace SCLauncher.backend.util;

/// <summary>Bridges Push and Pull async models.</summary>
/// Push-based callbacks send data whenever it arrives, but IAsyncEnumerable requires a consumer
/// to pull items asynchronously using await foreach.
/// System.Threading.Channels acts as a middleman queue.
/// A callback writes incoming data into the channel, and an IAsyncEnumerable reads from the channel.
public sealed class AsyncEnumerableBuffer<T> : IProgress<T>
{
	private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

	public void Push(T item)
	{
		_channel.Writer.TryWrite(item);
	}

	public void Complete()
	{
		_channel.Writer.Complete();
	}

	public IAsyncEnumerable<T> ReadAllAsync(CancellationToken ct = default)
	{
		return _channel.Reader.ReadAllAsync(ct);
	}

	void IProgress<T>.Report(T value) => Push(value);
}