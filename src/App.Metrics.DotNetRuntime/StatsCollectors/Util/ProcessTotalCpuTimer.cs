using System;
using System.Diagnostics;

namespace App.Metrics.DotNetRuntime.StatsCollectors.Util
{
    /// <summary>
    /// Helps calculate the the cpu process time used.
    /// </summary>
    public class ProcessTotalCpuTimer
    {
        private TimeSpan _lastProcessorUsedTime;
        private DateTime _timeOfLastCollection;
        private Process _process;

        internal ProcessTotalCpuTimer()
        {
            _process = Process.GetCurrentProcess();
            _lastProcessorUsedTime = GetProcessorTime();
            _timeOfLastCollection = DateTime.UtcNow;
        }

        private TimeSpan GetProcessorTime()
        {
            return _process.TotalProcessorTime;
        }

        public void Calculate()
        {
            var currentProcessTime = GetProcessorTime();
            ProcessTimeUsed = currentProcessTime - _lastProcessorUsedTime;
            _lastProcessorUsedTime = currentProcessTime;

            var now = DateTime.UtcNow;
            var timeElapsed = now.Subtract(_timeOfLastCollection).TotalMilliseconds;
            _timeOfLastCollection = DateTime.UtcNow;
            ProcessCpuUsedRatio = ProcessTimeUsed.TotalMilliseconds / (Environment.ProcessorCount * timeElapsed);
        }

        public double ProcessCpuUsedRatio { get; private set; }
        public TimeSpan ProcessTimeUsed { get; private set; }
    }
}
