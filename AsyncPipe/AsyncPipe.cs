using Nito.AsyncEx;

namespace AsyncPipe;

#pragma warning disable CA1844
public class AsyncPipe : Stream
#pragma warning restore CA1844
{
	private readonly record struct ToWriteData(byte[] Data, int Offset, int Count);

	public override bool CanRead => true;
	public override bool CanSeek => false;
	public override bool CanWrite => true;
	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	private readonly AsyncLock _locker = new();
	private readonly UnbufferedChannel<ToWriteData> _toWriteBytes = new();
	private readonly UnbufferedChannel<int> _readCount = new();

	/// <summary>
	/// Flush is no op in pipe
	/// </summary>
	public override void Flush()
	{
		// no op
	}

	/// <summary>
	/// Seek does not work in pipe
	/// </summary>
	/// <param name="offset"></param>
	/// <param name="origin"></param>
	/// <returns></returns>
	/// <exception cref="NotSupportedException"></exception>
	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// We cannot set the length in pipe
	/// </summary>
	/// <param name="value"></param>
	/// <exception cref="NotSupportedException">Always is thrown</exception>
	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		// Get data from channel
		var toWrite = _toWriteBytes.Receive();
		// Try to write it into buffer
		int copied = Copy(new ToWriteData(buffer, offset, count), toWrite);
		_readCount.Send(copied);
		// Done
		return copied;
	}

	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		// Get data from channel
		var toWrite = await _toWriteBytes.ReceiveAsync();
		// Try to write it into buffer
		int copied = Copy(new ToWriteData(buffer, offset, count), toWrite);
		await _readCount.SendAsync(copied);
		// Done
		return copied;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		using var l = _locker.Lock();
		while (count > 0)
		{
			// Send the data
			_toWriteBytes.Send(new ToWriteData(buffer, offset, count));
			// Get the written amount
			int readBytesCount = _readCount.Receive();
			// Move offset and count
			offset += readBytesCount;
			count -= readBytesCount;
		}
	}

	public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		using var l = await _locker.LockAsync(cancellationToken);
		while (count > 0)
		{
			// Send the data
			await _toWriteBytes.SendAsync(new ToWriteData(buffer, offset, count));
			// Get the written amount
			int readBytesCount = await _readCount.ReceiveAsync();
			// Move offset and count
			offset += readBytesCount;
			count -= readBytesCount;
		}
	}

	/// <summary>
	/// Copy will copy the bytes from a source to a destination
	/// </summary>
	/// <param name="destination">The destination to copy the bytes to</param>
	/// <param name="source">The source of data to copy from</param>
	/// <returns>Number of bytes copied</returns>
	private static int Copy(ToWriteData destination, ToWriteData source)
	{
		int count = Math.Min(source.Count, destination.Count);
		Buffer.BlockCopy(source.Data, source.Offset, destination.Data, destination.Offset, count);
		return count;
	}
}