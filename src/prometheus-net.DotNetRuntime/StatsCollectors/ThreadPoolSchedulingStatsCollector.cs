using System;
using System.Diagnostics.Tracing;
using System.Linq;
using App.Metrics;
using Prometheus.DotNetRuntime.EventSources;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures the volume of work scheduled on the thread pool and the delay between scheduling the work and it beginning execution.
    /// </summary>
    internal sealed class ThreadPoolSchedulingStatsCollector : IEventSourceStatsCollector
    {
        private const int EventIdThreadPoolEnqueueWork = 30, EventIdThreadPoolDequeueWork = 31;
        private readonly double[] _histogramBuckets;
        private readonly SamplingRate _samplingRate;
        private readonly IMetrics _metrics;

        private readonly EventPairTimer<long> _eventPairTimer;

        public ThreadPoolSchedulingStatsCollector(double[] histogramBuckets, SamplingRate samplingRate, IMetrics metrics)
        {
            _histogramBuckets = histogramBuckets;
            _samplingRate = samplingRate;
            _metrics = metrics;
            _eventPairTimer  = new EventPairTimer<long>(
                EventIdThreadPoolEnqueueWork, 
                EventIdThreadPoolDequeueWork, 
                x => (long)x.Payload[0],
                samplingRate,
                new Cache<long, int>(TimeSpan.FromSeconds(30), initialCapacity: 512)
            );
        }

        internal ThreadPoolSchedulingStatsCollector(IMetrics metrics): this(Constants.DefaultHistogramBuckets, SampleEvery.OneEvent, metrics)
        {
        }

        public EventKeywords Keywords => (EventKeywords) (FrameworkEventSource.Keywords.ThreadPool);
        public EventLevel Level => EventLevel.Verbose;
        public Guid EventSourceGuid => FrameworkEventSource.Id;
        
        public void ProcessEvent(EventWrittenEventArgs e)
        {
            switch (_eventPairTimer.TryGetDuration(e, out var duration))
            {
                case DurationResult.Start:
                    _metrics.Measure.Meter.Mark(DotNetRuntimeMetricsRegistry.Meters.ScheduledCount);
                    return;
                
                case DurationResult.FinalWithDuration:
                    _metrics.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.ScheduleDelay).Record((duration.TotalMilliseconds * _samplingRate.SampleEvery).RoundToLong(), TimeUnit.Milliseconds);
                    return;
                
                default:
                    return;
            }
        }
    }
}