using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using AppMetrics.DotNetRuntime.StatsCollectors;
using AppMetrics.DotNetRuntime.StatsCollectors.Util;

namespace AppMetrics.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    [TestFixture]
    internal class Given_A_ThreadPoolSchedulingStatsCollector_That_Samples_Every_Event : StatsCollectorIntegrationTestBase<ThreadPoolSchedulingStatsCollector>
    {
        protected override ThreadPoolSchedulingStatsCollector CreateStatsCollector()
        {
            return new ThreadPoolSchedulingStatsCollector(MetricsClient);
        }

        [Test]
        [Repeat(5)]
        public async Task When_work_is_queued_on_the_thread_pool_then_the_queued_and_scheduled_work_is_measured()
        {
            MetricsClient.Provider.Meter.Instance(DotNetRuntimeMetricsRegistry.Meters.ScheduledCount).Reset();
            // act (Task.Run will execute the function on the thread pool)
            // There seems to be either a bug in the implementation of .NET core or a bug in my understanding...
            // First call to Task.Run triggers a queued event but not a queue event. For now, call twice 
            await Task.Run(() => 1 );
            var sp = Stopwatch.StartNew();
            await Task.Run(() => sp.Stop());
            sp.Stop();
            
            Assert.That(GetMeter(DotNetRuntimeMetricsRegistry.Meters.ScheduledCount.Name).Value.Count, 
                Is.GreaterThanOrEqualTo(1).After(100, 10));
            Assert.That(GetTimer(DotNetRuntimeMetricsRegistry.Timers.ScheduleDelay.Name).Value.Histogram.Count,
                Is.GreaterThanOrEqualTo(1));
            Assert.That(GetTimer(DotNetRuntimeMetricsRegistry.Timers.ScheduleDelay.Name).Value.Histogram.Sum,
                Is.EqualTo(sp.Elapsed.Milliseconds).Within(100.0));
        }
    }
    
    [TestFixture]
    internal class Given_A_ThreadPoolSchedulingStatsCollector_That_Samples_Fifth_Event : StatsCollectorIntegrationTestBase<ThreadPoolSchedulingStatsCollector>
    {
        protected override ThreadPoolSchedulingStatsCollector CreateStatsCollector()
        {
            return new ThreadPoolSchedulingStatsCollector(MetricsClient);
        }

        [Test]
        public async Task When_many_items_of_work_is_queued_on_the_thread_pool_then_the_queued_and_scheduled_work_is_measured()
        {
            MetricsClient.Provider.Meter.Instance(DotNetRuntimeMetricsRegistry.Meters.ScheduledCount).Reset();
         
            // act (Task.Run will execute the function on the thread pool)
            // There seems to be either a bug in the implementation of .NET core or a bug in my understanding...
            // First call to Task.Run triggers a queued event but not a queue event. For now, call twice 
            await Task.Run(() => 1 );

            var sp = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                sp.Start();
                await Task.Run(() => sp.Stop());
            }
            
            Assert.That(GetMeter(DotNetRuntimeMetricsRegistry.Meters.ScheduledCount.Name).Value.Count, 
                Is.GreaterThanOrEqualTo(100).After(100, 10));

            Assert.That(GetTimer(DotNetRuntimeMetricsRegistry.Timers.ScheduleDelay.Name).Value.Histogram.Count,
                Is.GreaterThanOrEqualTo(100));
            Assert.That(GetTimer(DotNetRuntimeMetricsRegistry.Timers.ScheduleDelay.Name).Value.Histogram.Sum,
                Is.EqualTo(sp.Elapsed.TotalMilliseconds).Within(100).After(100, 10));
        }
    }
}