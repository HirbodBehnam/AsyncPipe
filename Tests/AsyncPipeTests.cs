using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Tests;

public class AsyncPipeTests
{
	[Test]
	public void TestPipeNormal()
	{
		AsyncPipe.AsyncPipe pipe = new();
		const string data = "hello world";
		var readBuffer = new byte[data.Length];
		var readSize = 0;
		var t1 = new Thread(() => pipe.Write(Encoding.ASCII.GetBytes(data)));
		var t2 = new Thread(() => Interlocked.Exchange(ref readSize, pipe.Read(readBuffer)));
		t1.Start();
		t2.Start();
		t1.Join();
		t2.Join();
		Assert.AreEqual(data.Length, readSize);
		Assert.AreEqual(data, Encoding.ASCII.GetString(readBuffer));
	}

	[Test]
	public void TestPipeChunked()
	{
		AsyncPipe.AsyncPipe pipe = new();
		const string data = "hello world";
		var readBuffer = new List<byte>(data.Length);
		var t1 = new Thread(() => pipe.Write(Encoding.ASCII.GetBytes(data)));
		var t2 = new Thread(() =>
		{
			var buffer = new byte[1];
			for (var i = 0; i < data.Length; i++)
			{
				pipe.Read(buffer);
				readBuffer.Add(buffer[0]);
			}
		});
		t1.Start();
		t2.Start();
		t1.Join();
		t2.Join();
		Assert.AreEqual(data, Encoding.ASCII.GetString(readBuffer.ToArray()));
	}
}