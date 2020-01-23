using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using App.Metrics;
using App.Metrics.Counter;
using Prometheus.DotNetRuntime.StatsCollectors;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime
{
    internal sealed class DotNetEventListener : EventListener
    {
        private static CounterOptions _cpuConsumed;
        private static CounterOptions _eventTypeCounts;

        private readonly IEventSourceStatsCollector _collector;
        private readonly Action<Exception> _errorHandler;
        private readonly bool _enableDebugging;
        private readonly IMetrics _metrics;

        internal DotNetEventListener(IEventSourceStatsCollector collector, Action<Exception> errorHandler, bool enableDebugging, IMetrics metrics) : base()
        {
            _collector = collector;
            _errorHandler = errorHandler;
            _enableDebugging = enableDebugging;
            _metrics = metrics;

            if (_enableDebugging)
            {
                _cpuConsumed = new CounterOptions()
                {
                    Context = "DotNetRuntime",
                    MeasurementUnit = Unit.None,
                    ReportItemPercentages = false,
                    Name = "dotnet_debug_cpu_milliseconds_total",
                    Tags = new MetricTags("collector", collector.GetType().Name.ToSnakeCase())
                };
            }

            _eventTypeCounts = new CounterOptions()
            {
                Context = "DotNetRuntime",
                MeasurementUnit = Unit.Items,
                ReportItemPercentages = false
            };
            
            EnableEventSources(collector);
        }
        
        internal bool StartedReceivingEvents { get; private set; }

        private void EnableEventSources(IEventSourceStatsCollector forCollector)
        {
            EventSourceCreated += (sender, e) =>
            {
                var es = e.EventSource;
                if (es.Guid == forCollector.EventSourceGuid)
                {
                    EnableEvents(es, forCollector.Level, forCollector.Keywords);
                    StartedReceivingEvents = true;
                }
            };
        }
        
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var sp = new Stopwatch();
            try
            {
                if (_enableDebugging)
                {
                    _metrics.Measure.Counter.Increment(_eventTypeCounts, new MetricTags(new[]{"EventSource", "EventName"}, new []{eventData.EventSource.Name, eventData.EventName}));
                    sp.Restart();
                }
                
                _collector.ProcessEvent(eventData);

                if (_enableDebugging)
                {
                    sp.Stop();
                    
                    _metrics.Measure.Counter.Increment(_cpuConsumed, new MetricTags(new[]{"EventSource", "EventName"}, new []{eventData.EventSource.Name, eventData.EventName}), sp.Elapsed.TotalMilliseconds.RoundToLong());
                }
            }
            catch (Exception e)
            {
                _errorHandler(e);
            }
        }
    }
}