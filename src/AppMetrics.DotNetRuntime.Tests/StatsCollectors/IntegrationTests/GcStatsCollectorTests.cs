using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Gauge;
using App.Metrics.Meter;
using App.Metrics.Reporting;
using App.Metrics.Timer;
using NUnit.Framework;
using AppMetrics.DotNetRuntime.StatsCollectors;

namespace AppMetrics.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    internal class GcStatsCollectorTests : StatsCollectorIntegrationTestBase<GcStatsCollector>
    {
        protected override GcStatsCollector CreateStatsCollector()
        {
            return new GcStatsCollector(MetricsClient);
        }

        [Test]
        public void When_100kb_of_small_objects_are_allocated_then_the_allocated_bytes_counter_is_increased()
        {
            
            var previousValue = MetricsClient.Provider.Meter.Instance(DotNetRuntimeMetricsRegistry.Meters.AllocatedBytes, new MetricTags("heap", "soh")).GetValueOrDefault().Count;
            
            // allocate roughly 100kb+ of small objects
            for (int i = 0; i < 11; i++)
            {
                var b = new byte[10_000];
            }

            Assert.That(() => MetricsClient.Provider.Meter.Instance(DotNetRuntimeMetricsRegistry.Meters.AllocatedBytes, new MetricTags("heap", "soh")).GetValueOrDefault().Count,
                Is.GreaterThanOrEqualTo(previousValue + 100_000).After(500, 10));
        }

        [Test]
        public void When_a_100kb_large_object_is_allocated_then_the_allocated_bytes_counter_is_increased()
        {
            var previousValue = MetricsClient.Provider.Meter.Instance(DotNetRuntimeMetricsRegistry.Meters.AllocatedBytes, new MetricTags("heap", "loh")).GetValueOrDefault().Count;

            // allocate roughly 100kb+ of large objects
            var b = new byte[110_000];

            Assert.That(() => MetricsClient.Provider.Meter.Instance(DotNetRuntimeMetricsRegistry.Meters.AllocatedBytes, new MetricTags("heap", "loh")).GetValueOrDefault().Count,
                Is.GreaterThanOrEqualTo(previousValue + 100_000).After(500, 10));
        }

        [Test]
        public void When_a_garbage_collection_is_performed_then_the_heap_sizes_are_updated()
        {
            unsafe
            {
                // arrange (fix a variable to ensure the pinned objects counter is incremented
                var b = new byte[1];
                fixed (byte* p = b)
                {
                    // act
                    GC.Collect(0);
                }
                
                Task.WaitAll((MetricsClient.ReportRunner.RunAllAsync().ToArray()));

                Assert.That(() => GetGauage(DotNetRuntimeMetricsRegistry.Gauges.GcHeapSizeBytes.Name, "generation:0").Value,
                    Is.GreaterThan(0).After(200, 10));
                Assert.That(() => GetGauage(DotNetRuntimeMetricsRegistry.Gauges.GcHeapSizeBytes.Name, "generation:1").Value,
                    Is.GreaterThan(0).After(200, 10));
                Assert.That(() => GetGauage(DotNetRuntimeMetricsRegistry.Gauges.GcHeapSizeBytes.Name, "generation:2").Value,
                    Is.GreaterThan(0).After(200, 10));
                Assert.That(() => GetGauage(DotNetRuntimeMetricsRegistry.Gauges.GcHeapSizeBytes.Name, "generation:loh").Value,
                    Is.GreaterThan(0).After(200, 10));
                
                Assert.That(() => GetGauage(DotNetRuntimeMetricsRegistry.Gauges.GcNumPinnedObjects.Name)
                        .Value,
                    Is.GreaterThan(0).After(200, 10));
            }
        }
       
        
        [Test]
        public void When_a_garbage_collection_is_performed_then_the_finalization_queue_is_updated()
        {
            MetricsClient.Provider.Gauge.Instance(DotNetRuntimeMetricsRegistry.Gauges.GcFinalizationQueueLength)
                .Reset();
            
            // arrange
            {
                var finalizable = new FinalizableTest();
                finalizable = null;
            }
            GC.Collect();
            
            Assert.That(() => GetGauage(DotNetRuntimeMetricsRegistry.Gauges.GcFinalizationQueueLength.Name).Value,
                Is.GreaterThan(0).After(2000, 10));
        }

        [Test]
        public void When_a_garbage_collection_is_performed_then_the_collection_and_pause_stats_and_reasons_are_updated()
        {
            // arrange
            GC.Collect(1, GCCollectionMode.Forced);
            GC.Collect(2, GCCollectionMode.Forced, true, true);
         
            Task.WaitAll((MetricsClient.ReportRunner.RunAllAsync().ToArray()));
            
            // assert
            Assert.That(() => MetricsClient.Snapshot.GetForContext(DotNetRuntimeMetricsRegistry.ContextName)
                    .Timers.Single( g=> g.MultidimensionalName == DotNetRuntimeMetricsRegistry.Timers.GcCollectionMilliSeconds.Name)
                    .Value.Histogram.Count,
                Is.GreaterThanOrEqualTo(1)); // at least 3 generations
            // Assert.That(() => StatsCollector.GcCollectionSeconds.CollectAllCountValues().Count(), 
            //     Is.GreaterThanOrEqualTo(1).After(500, 10)); // at least 3 generations
            // Assert.That(() => StatsCollector.GcCollectionSeconds.CollectAllSumValues(excludeUnlabeled: true), Is.All.GreaterThan(0));
            // Assert.That(() => StatsCollector.GcCollectionReasons.CollectAllValues(excludeUnlabeled: true), Is.All.GreaterThan(0));
            // Assert.That(() => StatsCollector.GcPauseSeconds.CollectAllSumValues().Single(), Is.GreaterThan(0).After(500, 10));
        }
        //
        // [Test]
        // public void When_a_garbage_collection_is_performed_then_the_gc_cpu_and_pause_ratios_can_be_calculated()
        // {
        //     // arrange
        //     GC.Collect(2, GCCollectionMode.Forced, true, true);
        //
        //     Assert.That(() => StatsCollector.GcPauseSeconds.CollectAllCountValues().First(), Is.GreaterThan(0).After(2000, 10));
        //     Assert.That(()=> StatsCollector.GcCollectionSeconds.CollectAllSumValues().Sum(x => x), Is.GreaterThan(0).After(2000, 10));
        //     
        //     // To improve the reliability of the test, do some CPU busy work + call UpdateMetrics here.
        //     // Why? Process.TotalProcessorTime isn't very precise (it's not updated after every small bit of CPU consumption)
        //     // and this can lead to CpuRatio believing that no CPU has been consumed
        //     long i = 2_000_000_000;
        //     while (i > 0)
        //         i--;
        //
        //     // act 
        //     StatsCollector.UpdateMetrics();
        //     
        //     // assert
        //     Assert.That(StatsCollector.GcPauseRatio.Value, Is.GreaterThan(0.0).After(1000, 1), "GcPauseRatio");
        //     Assert.That(StatsCollector.GcCpuRatio.Value, Is.GreaterThan(0.0).After(1000, 1), "GcCpuRatio");
        // }

        public class FinalizableTest
        {
            ~FinalizableTest()
            {
                // Sleep for a bit so our object won't exit the finalization queue immediately
                Thread.Sleep(1000);
            }
        }
    }
}