using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using AsyncPipe;

namespace Tests;

public class UnbufferedChannelTests
{
	[Test]
	public void TestSendWrite()
	{
		const int sentNumber = 1;
		var channel = new UnbufferedChannel<int>();
		int numberGot = sentNumber - 1;
		var t1 = new Thread(() => channel.Send(1));
		var t2 = new Thread(() => Interlocked.Exchange(ref numberGot, channel.Receive()));
		t1.Start();
		t2.Start();
		t1.Join();
		t2.Join();
		Assert.AreEqual(sentNumber, numberGot);
	}

	[Test]
	public void TestSendWriteWithDelayOnWrite()
	{
		var channel = new UnbufferedChannel<int>();
		const int sentNumber = 1, errorSpanMilliseconds = 20;
		var waitTime = TimeSpan.FromMilliseconds(500);
		int numberGot = sentNumber - 1, timeSpent = 0;
		var t1 = new Thread(() =>
		{
			Thread.Sleep(waitTime);
			channel.Send(1);
		});
		var t2 = new Thread(() =>
		{
			var startTime = DateTime.Now;
			Interlocked.Exchange(ref numberGot, channel.Receive());
			Interlocked.Exchange(ref timeSpent, (DateTime.Now - startTime).Milliseconds);
		});
		t1.Start();
		t2.Start();
		t1.Join();
		t2.Join();
		Assert.AreEqual(sentNumber, numberGot);
		Assert.LessOrEqual(Math.Abs(waitTime.Milliseconds - timeSpent), errorSpanMilliseconds);
	}

	[Test]
	public void TestSendWriteWithDelayOnRead()
	{
		var channel = new UnbufferedChannel<int>();
		const int sentNumber = 1, errorSpanMilliseconds = 20;
		var waitTime = TimeSpan.FromMilliseconds(500);
		int numberGot = sentNumber - 1, timeSpent = 0;
		var t1 = new Thread(() =>
		{
			var startTime = DateTime.Now;
			channel.Send(1);
			Interlocked.Exchange(ref timeSpent, (DateTime.Now - startTime).Milliseconds);
		});
		var t2 = new Thread(() =>
		{
			Thread.Sleep(waitTime);
			Interlocked.Exchange(ref numberGot, channel.Receive());
		});
		t1.Start();
		t2.Start();
		t1.Join();
		t2.Join();
		Assert.AreEqual(sentNumber, numberGot);
		Assert.LessOrEqual(Math.Abs(waitTime.Milliseconds - timeSpent), errorSpanMilliseconds);
	}

	[Test]
	public void TestSendWriteAsync()
	{
		const int sentNumber = 1;
		var channel = new UnbufferedChannel<int>();
		int numberGot = sentNumber - 1;
		var t1 = Task.Run(async () => await channel.SendAsync(1));
		var t2 = Task.Run(async () => Interlocked.Exchange(ref numberGot, await channel.ReceiveAsync()));
		t1.Wait();
		t2.Wait();
		Assert.AreEqual(sentNumber, numberGot);
	}

	[Test]
	public void TestSendWriteWithDelayOnWriteAsync()
	{
		var channel = new UnbufferedChannel<int>();
		const int sentNumber = 1, errorSpanMilliseconds = 20;
		var waitTime = TimeSpan.FromMilliseconds(500);
		int numberGot = sentNumber - 1, timeSpent = 0;
		var t1 = Task.Run(async () =>
		{
			await Task.Delay(waitTime);
			await channel.SendAsync(1);
		});
		var t2 = Task.Run(async () =>
		{
			var startTime = DateTime.Now;
			Interlocked.Exchange(ref numberGot, await channel.ReceiveAsync());
			Interlocked.Exchange(ref timeSpent, (DateTime.Now - startTime).Milliseconds);
		});
		t1.Wait();
		t2.Wait();
		Assert.AreEqual(sentNumber, numberGot);
		Assert.LessOrEqual(Math.Abs(waitTime.Milliseconds - timeSpent), errorSpanMilliseconds);
	}

	[Test]
	public void TestSendWriteWithDelayOnReadAsync()
	{
		var channel = new UnbufferedChannel<int>();
		const int sentNumber = 1, errorSpanMilliseconds = 20;
		var waitTime = TimeSpan.FromMilliseconds(500);
		int numberGot = sentNumber - 1, timeSpent = 0;
		var t1 = Task.Run(async () =>
		{
			var startTime = DateTime.Now;
			await channel.SendAsync(1);
			Interlocked.Exchange(ref timeSpent, (DateTime.Now - startTime).Milliseconds);
		});
		var t2 = Task.Run(async () =>
		{
			await Task.Delay(waitTime);
			Interlocked.Exchange(ref numberGot, await channel.ReceiveAsync());
		});
		t1.Wait();
		t2.Wait();
		Assert.AreEqual(sentNumber, numberGot);
		Assert.LessOrEqual(Math.Abs(waitTime.Milliseconds - timeSpent), errorSpanMilliseconds);
	}
}