using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.DotNetRuntime;
using App.Metrics.Gauge;
using App.Metrics.Meter;
using App.Metrics.Reporting.Console;
using App.Metrics.Timer;
using NUnit.Framework;
using AppMetrics.DotNetRuntime;

namespace AppMetrics.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    [TestFixture]
    internal abstract class StatsCollectorIntegrationTestBase<TStatsCollector> 
        where TStatsCollector : IEventSourceStatsCollector
    {
        private DotNetEventListener _eventListener;
        protected TStatsCollector StatsCollector { get; private set; }
        protected IMetricsRoot MetricsClient { get; private set; }

        [SetUp]
        public void SetUp()
        {
            MetricsClient = CreateMetricsClient();
            StatsCollector = CreateStatsCollector();
            _eventListener = new DotNetEventListener(StatsCollector, exception => Console.Write(exception.Message), true, MetricsClient);
            
            // wait for event listener thread to spin up
            while (!_eventListener.StartedReceivingEvents)
            {
                Thread.Sleep(10); 
                Console.Write("Waiting.. ");
                
            }
            Console.WriteLine("EventListener should be active now.");
        }

        [TearDown]
        public void TearDown()
        {
            _eventListener.Dispose();
        }

        protected abstract TStatsCollector CreateStatsCollector();

        protected virtual IMetricsRoot CreateMetricsClient()
        {
            return App.Metrics.AppMetrics.CreateDefaultBuilder().Report.Using<ConsoleMetricsReporter>(TimeSpan.FromMilliseconds(1000)).Build();
        }
        
        public MeterValueSource GetMeter(string name)
        {
            return MetricsClient.Snapshot.GetForContext(DotNetRuntimeMetricsRegistry.ContextName)
                .Meters.Single(g => g.MultidimensionalName == name);
        }
        
        public MeterValueSource GetMeter(string name, string substring)
        {
            return MetricsClient.Snapshot.GetForContext(DotNetRuntimeMetricsRegistry.ContextName)
                .Meters.Single(g => g.MultidimensionalName == name  && g.Tags.AsMetricName(name)
                                        .Contains(substring));
        }
        
        public TimerValueSource GetTimer(string name)
        {
            return MetricsClient.Snapshot.GetForContext(DotNetRuntimeMetricsRegistry.ContextName)
                .Timers.Single(g => g.MultidimensionalName == name);
        }
        
        public TimerValueSource GetTimer(string name, string substring)
        {
            return MetricsClient.Snapshot.GetForContext(DotNetRuntimeMetricsRegistry.ContextName)
                .Timers.Single(g => g.MultidimensionalName == name  && g.Tags.AsMetricName(name)
                                        .Contains(substring));
        }
        
        public GaugeValueSource GetGauage(string name)
        {
            return MetricsClient.Snapshot.GetForContext(DotNetRuntimeMetricsRegistry.ContextName)
                .Gauges.Single(g => g.MultidimensionalName == name);
        }
        
        public GaugeValueSource GetGauage(string name, string substring)
        {
            return MetricsClient.Snapshot.GetForContext(DotNetRuntimeMetricsRegistry.ContextName)
                .Gauges.Single(g => g.MultidimensionalName == name
                                    && g.Tags.AsMetricName(name)
                                        .Contains(substring)
                );
        }
    }
}