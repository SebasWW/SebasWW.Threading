using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SebasWW.Threading.UnitTests
{
	public class KeyedSemaphoreSlimExtensionTest
	{
		private KeyedSemaphoreSlim<int> scenarioSemaphore = new KeyedSemaphoreSlim<int>();

		private ConcurrentDictionary<int, int> dicCounter = new ConcurrentDictionary<int, int>();
		private ConcurrentDictionary<int, int> dicMax = new ConcurrentDictionary<int, int>();

		int counter2 = 0;
		int counter3 = 0;
		int maxWaiters = 0;


		[Fact]
		public void MultiArrayKey()
		{
			List<Thread> list = new List<Thread>();
			var rnd = new Random();

			for (var i = 0; i < 200; ++i)
			{
				byte[] b = new byte[50];
				rnd.NextBytes(b);

				var a = new Thread(() => TaskDo(b.Distinct().OrderBy(t => t).Select(t => (int)t)).GetAwaiter().GetResult());
				list.Add(a);
			}

			list.ForEach(t => t.Start());
			list.ForEach(t => t.Join());

			Assert.Equal(200, counter2);
			Assert.Equal(200, counter3);
			dicCounter.ToList().ForEach(item => Assert.Equal(0, item.Value));
			dicMax.ToList().ForEach(item => Assert.Equal(1, item.Value));
		}

		private async Task TaskDo(IEnumerable<int> keys)
		{
			lock (scenarioSemaphore)
			{
				counter2 += 1;
			}

			

			try
			{
				using (var sync = await scenarioSemaphore.WaitAsync(keys))
				{
					foreach(var key in keys)
					{
						await Task.Delay(100);

						var i = dicCounter.AddOrUpdate(key, 1, (i, y) => y + 1);
						if (dicCounter[key] > dicMax.GetOrAdd(key, 0)) dicMax[key] = dicCounter[key];



						var m = scenarioSemaphore.CurrentCount(key);
						if (m > maxWaiters) maxWaiters = m;
					}

					foreach (var key in keys)
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
