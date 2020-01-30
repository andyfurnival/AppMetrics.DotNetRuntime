using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Timer;
using AppMetrics.DotNetRuntime.StatsCollectors;
using AppMetrics.DotNetRuntime.StatsCollectors.Util;

namespace AppMetrics.DotNetRuntime
{
    internal sealed class DotNetEventListener : EventListener
    {
        private static TimerOptions _cpuConsumed;
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
                _cpuConsumed = new TimerOptions()
                {
                    Context = DotNetRuntimeMetricsRegistry.ContextName,
                    MeasurementUnit = Unit.None,
                    DurationUnit = TimeUnit.Nanoseconds,
                    Name = "dotnet_debug_cpu_nanoseconds_total",
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
            try
            {
                if (_enableDebugging)
                {
                    _metrics.Provider.Timer.Instance(_cpuConsumed,
                            new MetricTags(new[] {"EventSource", "EventName"},
                                new[] {eventData.EventSource.Name, eventData.EventName}))
                        .Time(() => _collector.ProcessEvent(eventData));
                    return;
                }

                _collector.ProcessEvent(eventData);
            }
            catch (Exception e)
            {
                _errorHandler(e);
            }
        }
    }
}