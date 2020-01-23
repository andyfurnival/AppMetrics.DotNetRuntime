using App.Metrics.Counter;
using App.Metrics.Gauge;
using App.Metrics.Histogram;

namespace Prometheus.DotNetRuntime
{
    public static class DotNetRuntimeMetricsRegistry
    {
        public static string ContextName = "DotNetRuntime";
        public static class Counters
        {
            public static readonly CounterOptions ContentionMilliSecondsTotal = new CounterOptions()
            {
                Context = ContextName,
                Name = "dotnet_contention_milliseconds_total",
                ResetOnReporting = true,
            };
            
            public static readonly CounterOptions ContentionTotal = new CounterOptions()
            {
                Context = ContextName,
                Name = "dotnet_contention_total",
                ResetOnReporting = true,
            };
            
            public static readonly CounterOptions GcCollectionReasons = new CounterOptions()
            {
                Context = ContextName,
                Name = "dotnet_gc_collection_reasons_total",
                ResetOnReporting = true,
            };
            
            public static readonly CounterOptions AllocatedBytes = new CounterOptions()
            {
                Context = ContextName,
                Name = "dotnet_gc_allocated_bytes_total",
                ResetOnReporting = true,
            };
            
            public static readonly CounterOptions MethodsJittedTotal = new CounterOptions()
            {
                Context = ContextName,
                Name = "dotnet_jit_method_total",
                ResetOnReporting = true,
            };
            
            public static readonly CounterOptions MethodsJittedMilliSecondsTotal = new CounterOptions()
            {
                Context = ContextName,
                Name = "dotnet_jit_method_milliseconds_total",
                ResetOnReporting = true,
            };
            
            public static readonly CounterOptions ScheduledCount = new CounterOptions()
            {
                Context = ContextName,
                Name = "dotnet_threadpool_scheduled_total",
                ResetOnReporting = true,
            };
            
            public static readonly CounterOptions AdjustmentsTotal = new CounterOptions()
            {
                Context = ContextName,
                Name = "dotnet_threadpool_adjustments_total",
                ResetOnReporting = true,
            };
        }

        public static class Timers
        {
            
        }

        public static class Gauges
        {
            public static readonly GaugeOptions GcCpuRatio = new GaugeOptions()
            {
                Context = ContextName,
                Name = "dotnet_gc_cpu_ratio"
            };
            
            public static readonly GaugeOptions GcPauseRatio = new GaugeOptions()
            {
                Context = ContextName,
                Name = "dotnet_gc_pause_ratio"
            };
            
            public static readonly GaugeOptions GcHeapSizeBytes = new GaugeOptions()
            {
                Context = ContextName,
                Name = "dotnet_gc_heap_size_bytes"
            };
            
            public static readonly GaugeOptions GcNumPinnedObjects = new GaugeOptions()
            {
                Context = ContextName,
                Name = "dotnet_gc_pinned_objects"
            };
            
            public static readonly GaugeOptions GcFinalizationQueueLength = new GaugeOptions()
            {
                Context = ContextName,
                Name = "dotnet_gc_finalization_queue_length"
            };
            
            public static readonly GaugeOptions CpuRatio = new GaugeOptions()
            {
                Context = ContextName,
                Name = "dotnet_jit_cpu_ratio"
            };
            
            public static readonly GaugeOptions NumThreads = new GaugeOptions()
            {
                Context = ContextName,
                Name = "dotnet_threadpool_num_threads"
            };
            
            public static readonly GaugeOptions NumIoThreads = new GaugeOptions()
            {
                Context = ContextName,
                Name = "dotnet_threadpool_num_io_threads"
            };//
        }


        public static class Histograms
        {
            public static readonly HistogramOptions GcPauseMilliSeconds = new HistogramOptions()
            {
                Context = ContextName,
                Name = "dotnet_gc_collection_milliseconds"
            };
            
            public static readonly HistogramOptions GcCollectionMilliSeconds = new HistogramOptions()
            {
                Context = ContextName,
                Name = "dotnet_gc_pause_milliseconds"
            };
            
            public static readonly HistogramOptions ScheduleDelay = new HistogramOptions()
            {
                Context = ContextName,
                Name = "dotnet_threadpool_scheduling_delay_milliseconds"
            }; 
        }
        
    }
}