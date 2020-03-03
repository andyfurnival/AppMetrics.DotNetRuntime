using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.DotNetRuntime;
using App.Metrics.DotNetRuntime.StatsCollectors;
using App.Metrics.Meter;
using App.Metrics.Timer;
using NUnit.Framework;

namespace AppMetrics.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    [TestFixture]
    internal class ContentionStatsCollectorTests : StatsCollectorIntegrationTestBase<ContentionStatsCollector>
    {
        protected override ContentionStatsCollector CreateStatsCollector()
        {
            return new ContentionStatsCollector(MetricsClient);
        }

        [Test]
        public void Will_measure_no_contention_on_an_uncontested_lock()
        {
            // arrange
            var key = new Object();
            
            // act
            lock (key)
            {
            }
            
            // assert
            Assert.That(MetricsClient.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.ContentionMilliSecondsTotal).GetValueOrDefault().Rate.Count, Is.EqualTo(0));
            Assert.That(MetricsClient.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.ContentionMilliSecondsTotal).GetValueOrDefault().Histogram.Sum, Is.EqualTo(0));
        }
        
        /// <summary>
        /// This test has the potential to be flaky (due to attempting to simulate lock contention across multiple threads in the thread pool),
        /// may have to revisit this in the future..
        /// </summary>
        /// <returns></returns>
        [Test]
        [Repeat(5)]
        public void Will_measure_contention_on_a_contested_lock()
        {
            var timer = MetricsClient.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.ContentionMilliSecondsTotal);
            timer.Reset();

            // arrange
            const int numThreads = 10;
            const int sleepForMs = 100;
            var key = new object();
            
            // Increase the min. thread pool size so that when we use Thread.Sleep, we don't run into scheduling delays
            ThreadPool.SetMinThreads(numThreads * 2, 1);

            // act
            var waitHandles = new WaitHandle[numThreads];
            for (var i = 0; i < numThreads; i++)
            {
                var handle = new EventWaitHandle(false, EventResetMode.ManualReset);
                var thread = new Thread(() =>
                {
                    lock (key)
                    {
                        Thread.Sleep(sleepForMs);
                    }

                    handle.Set();
                });
                waitHandles[i] = handle;
                thread.Start();
            }
            WaitHandle.WaitAll(waitHandles);

            // assert
            // Why -1? The first thread will not contend the lock 
            const int numLocksContended = numThreads - 1;
            Assert.That(timer.GetValueOrDefault().Rate.Count, Is.GreaterThanOrEqualTo(numLocksContended));

            // Pattern of expected contention times is: 50ms, 100ms, 150ms, etc.
            var expectedDelay = TimeSpan.FromMilliseconds(Enumerable.Range(1, numLocksContended).Aggregate(sleepForMs, (acc, next) => acc + (sleepForMs * next)));
            var actualValue = TimeSpan.FromMilliseconds(timer.GetValueOrDefault().Histogram.Sum / 1000D / 1000D);
            var within = TimeSpan.FromMilliseconds((sleepForMs + sleepForMs * 0.1D));
            Assert.That(actualValue, Is.EqualTo(expectedDelay).Within(within));
        }
    }
}