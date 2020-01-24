using System;
using System.Diagnostics.Tracing;
using App.Metrics;
using Prometheus.DotNetRuntime.EventSources;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures the activity of the JIT (Just In Time) compiler in a process.
    /// Tracks how often it runs and how long it takes to compile methods
    /// </summary>
    internal sealed class JitStatsCollector : IEventSourceStatsCollector
    {
        private readonly SamplingRate _samplingRate;
        private readonly IMetrics _metrics;
        private const int EventIdMethodJittingStarted = 145, EventIdMethodLoadVerbose = 143;
        private const string DynamicLabel = "dynamic";
        private const string LabelValueTrue = "true";
        private const string LabelValueFalse = "false";

        private readonly EventPairTimer<ulong> _eventPairTimer;

        private readonly Ratio _jitCpuRatio = Ratio.ProcessTotalCpu();

        public JitStatsCollector(SamplingRate samplingRate, IMetrics metrics)
        {
            _samplingRate = samplingRate;
            _metrics = metrics;
            _eventPairTimer = new EventPairTimer<ulong>(
                EventIdMethodJittingStarted,
                EventIdMethodLoadVerbose,
                x => (ulong)x.Payload[0],
                samplingRate
            );
        }
       
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.Jit;
        public EventLevel Level => EventLevel.Verbose;
        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (_eventPairTimer.TryGetDuration(e, out var duration) == DurationResult.FinalWithDuration)
            {
                // dynamic methods are of special interest to us- only a certain number of JIT'd dynamic methods
                // will be cached. Frequent use of dynamic can cause methods to be evicted from the cache and re-JIT'd
                var methodFlags = (uint)e.Payload[5];
                var dynamicLabelValue = (methodFlags & 0x1) == 0x1 ? LabelValueTrue : LabelValueFalse;
                
                _metrics.Measure.Counter.Increment(DotNetRuntimeMetricsRegistry.Counters.MethodsJittedTotal, new MetricTags(DynamicLabel, dynamicLabelValue), _samplingRate.SampleEvery);
                _metrics.Measure.Counter.Increment(DotNetRuntimeMetricsRegistry.Counters.MethodsJittedMilliSecondsTotal, new MetricTags(DynamicLabel, dynamicLabelValue), (duration.TotalMilliseconds * _samplingRate.SampleEvery).RoundToLong());
                
                var methodsJittedMsTotalCounter = _metrics.Provider.Counter.Instance(DotNetRuntimeMetricsRegistry.Counters.MethodsJittedMilliSecondsTotal);
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.CpuRatio, _jitCpuRatio.CalculateConsumedRatio(methodsJittedMsTotalCounter));
            }
        }
    }
}