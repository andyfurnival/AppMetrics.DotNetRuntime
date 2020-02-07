using App.Metrics.Gauge;
using App.Metrics.Timer;

namespace App.Metrics.DotNetRuntime
{
    public static class ProcessMetricsRegistry
    {
        private const string Context = "process";

        public static class Gauges
        {
            public static GaugeOptions CpuUsageRatio = new GaugeOptions
            {
                Context = Context,
                Name = "cpu_usage_ratio",
                MeasurementUnit = Unit.Percent
            };

            public static GaugeOptions ProcessPagedMemorySize = new GaugeOptions
            {
                Context = Context,
                Name = "paged_process_memory_size",
                MeasurementUnit = Unit.Bytes
            };

            public static GaugeOptions ProcessPeekPagedMemorySize = new GaugeOptions
            {
                Context = Context,
                Name = "peek_paged_memory_size",
                MeasurementUnit = Unit.Bytes
            };

            public static GaugeOptions ProcessPeekVirtualMemorySize = new GaugeOptions
            {
                Context = Context,
                Name = "peek_virtual_memory_size",
                MeasurementUnit = Unit.Bytes
            };

            public static GaugeOptions ProcessPeekWorkingSetSize = new GaugeOptions
            {
                Context = Context,
                Name = "peek_working_set",
                MeasurementUnit = Unit.Bytes
            };

            public static GaugeOptions ProcessWorkingSetSize = new GaugeOptions
            {
                Context = Context,
                Name = "working_set",
                MeasurementUnit = Unit.Bytes
            };

            public static GaugeOptions ProcessPrivateMemorySize = new GaugeOptions
            {
                Context = Context,
                Name = "private_memory_size",
                MeasurementUnit = Unit.Bytes
            };

            public static GaugeOptions ProcessVirtualMemorySize = new GaugeOptions
            {
                Context = Context,
                Name = "virtual_memory_size",
                MeasurementUnit = Unit.Bytes
            };

            public static GaugeOptions SystemNonPagedMemory = new GaugeOptions
            {
                Context = Context,
                Name = "system_non-paged_memory",
                MeasurementUnit = Unit.Bytes
            };

            public static GaugeOptions SystemPagedMemorySize = new GaugeOptions
            {
                Context = Context,
                Name = "system_paged_memory_size",
                MeasurementUnit = Unit.Bytes
            };

            public static GaugeOptions ThreadCount = new GaugeOptions
            {
                Context = Context,
                Name = "thread_count",
                MeasurementUnit = Unit.Threads
            };

            public static GaugeOptions HandlesCount = new GaugeOptions
            {
                Context = Context,
                Name = "handle_count",
                MeasurementUnit = Unit.Items
            };
        }

        public static class Timers
        {
            public static TimerOptions CpuUsedMilliseconds = new TimerOptions()
            {
                Context = Context,
                Name = "cpu_used_milliseconds",
                MeasurementUnit = Unit.None,
                DurationUnit = TimeUnit.Milliseconds,
                RateUnit = TimeUnit.Minutes
            };
        }
    }
}
