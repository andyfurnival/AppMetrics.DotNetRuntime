using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using App.Metrics;
using Prometheus.DotNetRuntime.EventSources;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures how the frequency and duration of garbage collections and volume of allocations. Includes information
    ///  such as the generation the collection is running for, what triggered the collection and the type of the collection.
    /// </summary>
    internal sealed class GcStatsCollector : IEventSourceStatsCollector
    {
        private const string
            LabelHeap = "gc_heap",
            LabelGeneration = "gc_generation",
            LabelReason = "gc_reason",
            LabelType = "gc_type";

        private const int
            EventIdGcStart = 1,
            EventIdGcStop = 2,
            EventIdSuspendEEStart = 9,
            EventIdRestartEEStop = 3,
            EventIdHeapStats = 4,
            EventIdAllocTick = 10;

        private readonly EventPairTimer<uint, GcData> _gcEventTimer = new EventPairTimer<uint, GcData>(
            EventIdGcStart,
            EventIdGcStop,
            x => (uint) x.Payload[0],
            x => new GcData((uint) x.Payload[1], (DotNetRuntimeEventSource.GCType) x.Payload[3]),
            SampleEvery.OneEvent);

        private readonly EventPairTimer<int> _gcPauseEventTimer = new EventPairTimer<int>(
            EventIdSuspendEEStart,
            EventIdRestartEEStop,
            // Suspensions/ Resumptions are always done sequentially so there is no common value to match events on. Return a constant value as the event id.
            x => 1,
            SampleEvery.OneEvent);

        private readonly Dictionary<DotNetRuntimeEventSource.GCReason, string> _gcReasonToLabels = LabelGenerator.MapEnumToLabelValues<DotNetRuntimeEventSource.GCReason>();
        private readonly Ratio _gcCpuRatio = Ratio.ProcessTotalCpu();
        private readonly Ratio _gcPauseRatio = Ratio.ProcessTime();
        private readonly double[] _histogramBuckets;
        private readonly IMetrics _metrics;

        public GcStatsCollector(double[] histogramBuckets, IMetrics metrics)
        {
            _histogramBuckets = histogramBuckets;
            _metrics = metrics;
        }

        public GcStatsCollector(IMetrics metrics) : this(Constants.DefaultHistogramBuckets, metrics)
        {
        }

        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.GC;
        public EventLevel Level => EventLevel.Verbose;

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (e.EventId == EventIdAllocTick)
            {
                const uint lohHeapFlag = 0x1;
                var heapLabelValue = ((uint) e.Payload[1] & lohHeapFlag) == lohHeapFlag ? "loh" : "soh";
                _metrics.Measure.Counter.Increment(DotNetRuntimeMetricsRegistry.Counters.AllocatedBytes, new MetricTags("heap", heapLabelValue), (uint)e.Payload[0]);
                return;
            }

            if (e.EventId == EventIdHeapStats)
            {
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcHeapSizeBytes, new MetricTags("generation", "0"), (UInt64)e.Payload[0]);
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcHeapSizeBytes, new MetricTags("generation", "1"), (UInt64)e.Payload[2]);
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcHeapSizeBytes, new MetricTags("generation", "2"), (UInt64)e.Payload[4]);
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcHeapSizeBytes, new MetricTags("generation", "loh"), (UInt64)e.Payload[6]);
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcFinalizationQueueLength, (UInt64)e.Payload[9]);
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcNumPinnedObjects, (UInt32)e.Payload[10]);
                return;
            }

            // flags representing the "Garbage Collection" + "Preparation for garbage collection" pause reasons
            const uint suspendGcReasons = 0x1 | 0x6;

            if (e.EventId == EventIdSuspendEEStart && ((uint) e.Payload[0] & suspendGcReasons) == 0)
            {
                // Execution engine is pausing for a reason other than GC, discard event.
                return;
            }

            if (_gcPauseEventTimer.TryGetDuration(e, out var pauseDuration) == DurationResult.FinalWithDuration)
            {
                var gcPauseMilliSecondsHistogram =
                    _metrics.Provider.Histogram.Instance(DotNetRuntimeMetricsRegistry.Histograms.GcPauseMilliSeconds);
                 gcPauseMilliSecondsHistogram.Update(pauseDuration.TotalMilliseconds.RoundToLong());
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcPauseRatio, _gcPauseRatio.CalculateConsumedRatio(gcPauseMilliSecondsHistogram));
                return;
            }

            if (e.EventId == EventIdGcStart)
            {
                _metrics.Measure.Counter.Increment(DotNetRuntimeMetricsRegistry.Counters.GcCollectionReasons, new MetricTags("reason", _gcReasonToLabels[(DotNetRuntimeEventSource.GCReason) e.Payload[2]]));
            }

            if (_gcEventTimer.TryGetDuration(e, out var gcDuration, out var gcData) == DurationResult.FinalWithDuration)
            {
                var gcCollectionMilliSecondsHistogram =
                    _metrics.Provider.Histogram.Instance(DotNetRuntimeMetricsRegistry.Histograms.GcCollectionMilliSeconds);
                gcCollectionMilliSecondsHistogram.Update(gcDuration.TotalMilliseconds.RoundToLong());
                _metrics.Measure.Gauge.SetValue(DotNetRuntimeMetricsRegistry.Gauges.GcCpuRatio, _gcCpuRatio.CalculateConsumedRatio(gcCollectionMilliSecondsHistogram));
            }
        }

        private struct GcData
        {
            private static readonly Dictionary<DotNetRuntimeEventSource.GCType, string> GcTypeToLabels = LabelGenerator.MapEnumToLabelValues<DotNetRuntimeEventSource.GCType>();

            public GcData(uint generation, DotNetRuntimeEventSource.GCType type)
            {
                Generation = generation;
                Type = type;
            }

            public uint Generation { get; }
            public DotNetRuntimeEventSource.GCType Type { get; }

            public string GetTypeToString()
            {
                return GcTypeToLabels[Type];
            }

            public string GetGenerationToString()
            {
                if (Generation > 2)
                {
                    return "loh";
                }

                return Generation.ToString();
            }
        }
    }
}