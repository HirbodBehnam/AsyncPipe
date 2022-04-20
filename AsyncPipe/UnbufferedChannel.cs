using System.Threading.Channels;
using Nito.AsyncEx;

namespace AsyncPipe;

internal class UnbufferedChannel<T>
{
	private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();
	private readonly AsyncLock _mutex;
	private readonly AsyncConditionVariable _dataReceived;
	private int _readersReady;

	public UnbufferedChannel()
	{
		_mutex = new AsyncLock();
		_dataReceived = new AsyncConditionVariable(_mutex);
	}

	// https://stackoverflow.com/a/16656629/4213397

	/// <summary>
	/// Sends data in channel
	/// </summary>
	/// <param name="data">The data to send</param>
	public void Send(T data)
	{
		// Lock everything
		using var locker = _mutex.Lock();
		// Wait until a reader is ready
		while (_readersReady == 0)
			_dataReceived.Wait();
		// Now that the reader is ready send the data in channel. This will be received in reader thread
		_channel.Writer.TryWrite(data);
		_dataReceived.NotifyAll(); // notify to wake up one of the readers
	}

	/// <summary>
	/// Asynchronously sends data in channel
	/// </summary>
	/// <param name="data">The data to send</param>
	public async Task SendAsync(T data)
	{
		// Lock everything
		using var locker = await _mutex.LockAsync();
		// Wait until a reader is ready
		while (_readersReady == 0)
			await _dataReceived.WaitAsync();
		// Now that the reader is ready send the data in channel. This will be received in reader thread
		_channel.Writer.TryWrite(data);
		_dataReceived.NotifyAll(); // notify to wake up one of the readers
	}

	/// <summary>
	/// Gets one data from channel
	/// </summary>
	/// <returns>The data in channel</returns>
	public T Receive()
	{
		// Lock everything
		using var locker = _mutex.Lock();
		// Make the reader available
		_readersReady++;
		_dataReceived.NotifyAll(); // notify the writers
		// Wait for a something in channel
		T? item;
		while (!_channel.Reader.TryRead(out item))
			_dataReceived.Wait();
		// We are done
		_readersReady--;
		return item;
	}

	/// <summary>
	/// Asynchronously gets one data from channel
	/// </summary>
	/// <returns>The data in channel</returns>
	public async Task<T> ReceiveAsync()
	{
		// Lock everything
		using var locker = await _mutex.LockAsync();
		// Make the reader available
		_readersReady++;
		_dataReceived.NotifyAll(); // notify the writers
		// Wait for a something in channel
		T? item;
		while (!_channel.Reader.TryRead(out item))
			await _dataReceived.WaitAsync();
		// We are done
		_readersReady--;
		return item;
	}
}