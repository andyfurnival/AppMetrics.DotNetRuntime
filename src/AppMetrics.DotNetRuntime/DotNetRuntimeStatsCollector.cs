using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using App.Metrics;
using App.Metrics.Gauge;
using App.Metrics.Registry;

namespace AppMetrics.DotNetRuntime
{
    internal sealed class DotNetRuntimeStatsCollector : 
        IDisposable

    {
        private static readonly Dictionary<IMetrics, DotNetRuntimeStatsCollector> Instances = new Dictionary<IMetrics, DotNetRuntimeStatsCollector>();
        
        private DotNetEventListener[] _eventListeners;
        private readonly ImmutableHashSet<IEventSourceStatsCollector> _statsCollectors;
        private readonly bool _enabledDebugging;
        private readonly Action<Exception> _errorHandler;
        private readonly IMetrics _metrics;
        private readonly object _lockInstance = new object();

        internal DotNetRuntimeStatsCollector(ImmutableHashSet<IEventSourceStatsCollector> statsCollectors, Action<Exception> errorHandler, bool enabledDebugging, IMetrics metrics)
        {
            _statsCollectors = statsCollectors;
            _enabledDebugging = enabledDebugging;
            _errorHandler = errorHandler ?? (e => { });
            _metrics = metrics;
            lock (_lockInstance)
            {
                if (Instances.ContainsKey(metrics))
                {
                    throw new InvalidOperationException(".NET runtime metrics are already being collected. Dispose() of your previous collector before calling this method again.");
                }

                Instances.Add(metrics, this);
            }
        }

        public void RegisterMetrics(IMetrics metrics)
        {
            // Metrics have been registered, start the event listeners
            _eventListeners = _statsCollectors
                .Select(sc => new DotNetEventListener(sc, _errorHandler, _enabledDebugging, metrics))
                .ToArray();

            SetupConstantMetrics(metrics);
        }
        
        public void Dispose()
        {
            try
            {
                if (_eventListeners == null)
                    return;

                foreach (var listener in _eventListeners)
                    listener?.Dispose();
            }
            finally
            {
                lock (_lockInstance)
                {
                    Instances.Remove(_metrics);
                }
            }
        }
        
        private void SetupConstantMetrics(IMetrics metrics)
        {
            // These metrics are fairly generic in name, catch any exceptions on trying to create them 
            // in case AppMetrics or another plugin has registered them.
            try
            {
                var buildInfo = new GaugeOptions()
                {
                    Context = "DotNetRuntime",
                    Name = "dotnet_build_info",
                    Tags = new MetricTags(
                        keys: new[] {"version", "target_framework", "runtime_version", "os_version", "process_architecture"},
                        values:new[]
                        {
                            this.GetType().Assembly.GetName().Version.ToString(),
                            Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName,
                            RuntimeInformation.FrameworkDescription,
                            RuntimeInformation.OSDescription,
                            RuntimeInformation.ProcessArchitecture.ToString()
                        })
                };
                
                metrics.Measure.Gauge.SetValue(buildInfo, 1);
            }
            catch (Exception e)
            {
                _errorHandler(e);
            }

            try
            {
                var cpuCount = new GaugeOptions()
                {
                    Context = "DotNetRuntime",
                    Name = "process_cpu_count"
                };
                
                metrics.Measure.Gauge.SetValue(cpuCount, Environment.ProcessorCount);
            }
            catch (Exception e)
            {
                _errorHandler(e);
            }
        }
    }
}