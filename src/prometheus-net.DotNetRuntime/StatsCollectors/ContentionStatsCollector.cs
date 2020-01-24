using System;
using System.Diagnostics.Tracing;
using App.Metrics;
using Prometheus.DotNetRuntime.EventSources;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures the level of contention in a .NET process, capturing the number 
    /// of locks contended and the total amount of time spent contending a lock.
    /// </summary>
    /// <remarks>
    /// Due to the way ETW events are triggered, only monitors contended will fire an event- spin locks, etc.
    /// do not trigger contention events and so cannot be tracked.
    /// </remarks>
    internal sealed class ContentionStatsCollector : IEventSourceStatsCollector
    {
        private readonly SamplingRate _samplingRate;
        private readonly IMetrics _metrics;
        private const int EventIdContentionStart = 81, EventIdContentionStop = 91;
        private readonly EventPairTimer<long> _eventPairTimer;

        public ContentionStatsCollector(SamplingRate samplingRate, IMetrics metrics)
        {
            _samplingRate = samplingRate;
            _metrics = metrics;
            _eventPairTimer = new EventPairTimer<long>(
                EventIdContentionStart,
                EventIdContentionStop,
                x => x.OSThreadId,
                samplingRate
            );
        }

        public EventKeywords Keywords => (EventKeywords)DotNetRuntimeEventSource.Keywords.Contention;
        public EventLevel Level => EventLevel.Informational;
        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;
        
        public void ProcessEvent(EventWrittenEventArgs e)
        {
            switch (_eventPairTimer.TryGetDuration(e, out var duration))
            {
                case DurationResult.Start:
                    _metrics.Measure.Meter.Mark(DotNetRuntimeMetricsRegistry.Meters.ContentionTotal);
                    return;
                
                case DurationResult.FinalWithDuration:
                    _metrics.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.ContentionMilliSecondsTotal).Record((long)(duration.TotalMilliseconds * _samplingRate.SampleEvery), TimeUnit.Milliseconds);
                    return;

                default:
                    return;
            }
        }
    }
}