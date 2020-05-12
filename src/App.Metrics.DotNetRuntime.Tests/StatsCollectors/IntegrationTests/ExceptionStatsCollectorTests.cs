using System;
using App.Metrics.DotNetRuntime;
using App.Metrics.DotNetRuntime.StatsCollectors;
using NUnit.Framework;

namespace AppMetrics.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    internal sealed class ExceptionStatsCollectorTests : StatsCollectorIntegrationTestBase<ExceptionStatsCollector>
    {
        [Test]
        public void When_exception_thrown_corresponded_counter_ticks()
        {
            MetricsClient.Provider.Meter.Instance(DotNetRuntimeMetricsRegistry.Meters.ExceptionsThrown).Reset();

            try
            {
                throw new InvalidOperationException("Expected exception");
            }
            catch
            {
            }

            Assert.That(() => GetMeter(DotNetRuntimeMetricsRegistry.Meters.ExceptionsThrown.Name).Value.Count,
                        Is.GreaterThanOrEqualTo(1).After(2000, 10));
        }

        protected override ExceptionStatsCollector CreateStatsCollector()
        {
            return new ExceptionStatsCollector(MetricsClient);
        }
    }
}
