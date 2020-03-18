using System;
using System.Diagnostics;
using System.Threading.Tasks;
using App.Metrics.DotNetRuntime.StatsCollectors.Util;
using App.Metrics.Scheduling;

namespace App.Metrics.DotNetRuntime.StatsCollectors
{
    public class ProcessInfoStatsCollector : IDisposable
    {
        private AppMetricsTaskScheduler  _scheduler;

        public ProcessInfoStatsCollector(IMetrics metrics)
        {
            var cpuUsage = new ProcessTotalCpuTimer();
            _scheduler = new AppMetricsTaskScheduler(
                    TimeSpan.FromMilliseconds(500),  () =>
                            {
                                var process = Process.GetCurrentProcess();
                                cpuUsage.Calculate();
                                metrics.Provider.Timer.Instance(ProcessMetricsRegistry.Timers.CpuUsedMilliseconds).Record(
                                    cpuUsage.ProcessTimeUsed.Ticks*100, TimeUnit.Nanoseconds);
                                metrics.Measure.Gauge.SetValue(ProcessMetricsRegistry.Gauges.CpuUsageRatio, () =>
                                {
                                    return cpuUsage.ProcessCpuUsedRatio;
                                });
                                metrics.Measure.Gauge.SetValue(ProcessMetricsRegistry.Gauges.ProcessPagedMemorySize, () => process.PagedMemorySize64);
                                metrics.Measure.Gauge.SetValue(ProcessMetricsRegistry.Gauges.ProcessPeekPagedMemorySize, () => process.PeakPagedMemorySize64);
                                metrics.Measure.Gauge.SetValue(ProcessMetricsRegistry.Gauges.ProcessPeekVirtualMemorySize, () => process.PeakVirtualMemorySize64);
                                metrics.Measure.Gauge.SetValue(ProcessMetricsRegistry.Gauges.ProcessWorkingSetSize, () => process.WorkingSet64);
                                metrics.Measure.Gauge.SetValue(ProcessMetricsRegistry.Gauges.ProcessPeekWorkingSetSize, () => process.PeakWorkingSet64);
                                metrics.Measure.Gauge.SetValue(ProcessMetricsRegistry.Gauges.ProcessPrivateMemorySize, () => process.PrivateMemorySize64);
                                metrics.Measure.Gauge.SetValue(ProcessMetricsRegistry.Gauges.ProcessVirtualMemorySize, () => process.VirtualMemorySize64);
                                metrics.Measure.Gauge.SetValue(ProcessMetricsRegistry.Gauges.SystemNonPagedMemory, () => process.NonpagedSystemMemorySize64);
                                metrics.Measure.Gauge.SetValue(ProcessMetricsRegistry.Gauges.SystemPagedMemorySize, () => process.PagedSystemMemorySize64);
                                metrics.Measure.Gauge.SetValue(ProcessMetricsRegistry.Gauges.ThreadCount, () => process.Threads.Count);
                                metrics.Measure.Gauge.SetValue(ProcessMetricsRegistry.Gauges.HandlesCount, () => process.HandleCount);
                                return Task.CompletedTask;
                            });
            _scheduler.Start();
        }

        public void Start()
        {
            _scheduler.Start();
        }

        public void Dispose()
        {
            _scheduler.Dispose();
        }
    }
}
