using System;
using System.Diagnostics;

namespace App.Metrics.DotNetRuntime.StatsCollectors.Util
{
    /// <summary>
    /// Helps calculate the the cpu process time used.
    /// </summary>
    public class ProcessTotalCpuTimer
    {
        private readonly Func<TimeSpan> _getElapsedTime;
        private TimeSpan _lastProcessTime;

        internal ProcessTotalCpuTimer(Func<TimeSpan> getElapsedTime)
        {
            _getElapsedTime = getElapsedTime;
            _lastProcessTime = _getElapsedTime();
        }

        /// <summary>
        /// Calculates the ratio of CPU time consumed by an activity.
        /// </summary>
        /// <returns></returns>
        public static ProcessTotalCpuTimer ProcessTotalCpu()
        {
            var p = Process.GetCurrentProcess();
            return new ProcessTotalCpuTimer(() =>
            {
                p.Refresh();
                return p.TotalProcessorTime;
            });
        }

        public double GetElapsedTime()
        {
            var currentProcessTime = _getElapsedTime();
            var consumedProcessTime = currentProcessTime - _lastProcessTime;

            _lastProcessTime = currentProcessTime;
            return consumedProcessTime.TotalMilliseconds;
        }
    }
}
