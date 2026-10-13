using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SCLauncher.backend.util.stream;

/// A stream wrapper with progress reporting and timeouts (read only)
public class ResponsiveStream(Stream delegateStream, TimeSpan? timeout) : Stream
{
	public TimeSpan? Timeout { get; set; } = timeout;
	
	public Stream Delegate { get; } = delegateStream;

	/// <summary>
	/// Gets or sets a value indicating whether the delegate stream should be disposed when this stream is disposed.
	/// </summary>
	public bool DisposeDelegate { get; set; } = true;
	
	private readonly long? _totalBytes;
	private long _bytesRead;
	private readonly IProgress<int>? _progress;
	private int _lastReportedPercent = -1;

	public ResponsiveStream(Stream delegateStream, TimeSpan? timeout, long? totalBytes, IProgress<int>? progress)
		: this(delegateStream, timeout)
	{
		_totalBytes = totalBytes;
		_progress = progress;
	}

	public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count)
		.ConfigureAwait(false).GetAwaiter().GetResult();

	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		if (Timeout.HasValue)
		{
			cts.CancelAfter(Timeout.Value);
		}

		try
		{
			int read = await Delegate.ReadAsync(buffer, offset, count, cts.Token).ConfigureAwait(false);
			
			if (read > 0 && _progress != null)
			{
				_bytesRead += read;
				
				if (_totalBytes is > 0)
				{
					// 1. Calculate current percentage as a whole integer
					int currentPercent = (int)(_bytesRead * 100 / _totalBytes.Value);
					
					// 2. Only report if the integer value has actually ticked up
					if (currentPercent != _lastReportedPercent)
					{
						_lastReportedPercent = currentPercent;
						_progress.Report(currentPercent);
					}
				}
			}

			return read;
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			throw new TimeoutException($"Stream read operation timed out after {Timeout?.TotalMilliseconds} ms of inactivity.");
		}
	}

	public override void Flush() => Delegate.Flush();
	public override Task FlushAsync(CancellationToken cancellationToken) => Delegate.FlushAsync(cancellationToken);
	public override long Seek(long offset, SeekOrigin origin) => Delegate.Seek(offset, origin);
	public override void SetLength(long value) { throw new NotSupportedException(); }
	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	public override bool CanRead => Delegate.CanRead;
	public override bool CanSeek => Delegate.CanSeek;
	public override bool CanWrite => false;
	public override bool CanTimeout => true;
	public override long Length => _totalBytes ?? Delegate.Length;
	public override long Position { get => Delegate.Position; set => Delegate.Position = value; }
	public override int ReadTimeout
	{
		get => (int?)Timeout?.TotalMilliseconds ?? -1;
		set => Timeout = value < 0 ? null : TimeSpan.FromMilliseconds(value);
	}
	
	protected override void Dispose(bool disposing)
	{
		if (disposing && DisposeDelegate)
		{
			Delegate.Dispose();
		}
		base.Dispose(disposing);
	}
	
	public override async ValueTask DisposeAsync()
	{
		if (DisposeDelegate)
		{
			await Delegate.DisposeAsync().ConfigureAwait(false);
		}
		base.Dispose(false);
	}

}