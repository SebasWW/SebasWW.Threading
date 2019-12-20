using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SebasWW.Threading;
using Xunit;

namespace SebasWW.Threading.UnitTests
{
	public class KeyedSemaphoreSlimTest
	{
		private KeyedSemaphoreSlim<int> scenarioSemaphore = new KeyedSemaphoreSlim<int>();

		private ConcurrentDictionary<int, int> dicCounter = new ConcurrentDictionary<int, int>();
		private ConcurrentDictionary<int, int> dicMax = new ConcurrentDictionary<int, int>();

		int counter2 = 0;
		int counter3 = 0;

		[Fact]
		public void SingleKey()
		{
			List<Thread> list = new List<Thread>();

			for (var i = 0; i < 200; ++i)
			{
				var a = new Thread( () => TaskDo(1).GetAwaiter().GetResult());
				list.Add(a);
			}

			list.ForEach(t => t.Start());
			list.ForEach(t => t.Join());

			Assert.Equal(200, counter2);
			Assert.Equal(200, counter3);
			dicCounter.ToList().ForEach(item => Assert.Equal(0, item.Value));
			dicMax.ToList().ForEach(item => Assert.Equal(1, item.Value));
		}

		[Fact]
		public void MultiKey()
		{
			List<Thread> list = new List<Thread>();
			var rnd = new Random();

			for (var i = 0; i < 200; ++i)
			{
				var a = new Thread(() => TaskDo(rnd.Next(0, 50)).GetAwaiter().GetResult());
				list.Add(a);
			}

			list.ForEach(t => t.Start());
			list.ForEach(t => t.Join());

			Assert.Equal(200, counter2);
			Assert.Equal(200, counter3);
			dicCounter.ToList().ForEach(item => Assert.Equal(0, item.Value));
			dicMax.ToList().ForEach(item => Assert.Equal(1, item.Value));
		}

		private async Task TaskDo(int key)
		{
			lock (scenarioSemaphore)
			{
				counter2 += 1;
			}

			await Task.Delay(100);

			try
			{
				using (var sync = await scenarioSemaphore.WaitAsync(key))
				{
					var i = dicCounter.AddOrUpdate(key, 1,  (i, y) => y + 1);
					if (dicCounter[key] > dicMax.GetOrAdd(key, 0)) dicMax[key] = dicCounter[key];
					dicCounter.AddOrUpdate(key, 1, (i, y) => y - 1);
				}
			}
			catch (Exception ex)
			{
				throw;
			}

			lock (scenarioSemaphore)
			{
				counter3 += 1;
			}
		}
	}
}
