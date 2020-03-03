using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using App.Metrics.DotNetRuntime.StatsCollectors;
using System.Linq;

namespace App.Metrics.DotNetRuntime
{
    /// <summary>
    /// Configures what .NET core runtime metrics will be collected.
    /// </summary>
    public static class DotNetRuntimeStatsBuilder
    {
        /// <summary>
        /// Includes all available .NET runtime metrics by default. Call <see cref="Builder.StartCollecting(IMetrics)"/>
        /// to begin collecting metrics.
        /// </summary>
        /// <returns></returns>
        public static Builder Default()
        {
            return Customize()
                .WithContentionStats()
                .WithJitStats()
                .WithThreadPoolSchedulingStats()
                .WithThreadPoolStats()
                .WithGcStats();
        }

        /// <summary>
        /// Allows you to customize the types of metrics collected.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Include specific .NET runtime metrics by calling the WithXXX() methods and then call <see cref="Builder.StartCollecting(IMetrics)"/>
        /// </remarks>
        public static Builder Customize()
        {
            return new Builder();
        }

        public class Builder
        {
            private Action<Exception> _errorHandler;
            private bool _debugMetrics;

            internal HashSet<Func<IMetrics, IEventSourceStatsCollector>> StatsCollectors { get; } = new HashSet<Func<IMetrics, IEventSourceStatsCollector>>();

            /// <summary>
            /// Finishes configuration and starts collecting .NET runtime metrics. Returns a <see cref="IDisposable"/> that
            /// can be disposed of to stop metric collection.
            /// </summary>
            /// <returns></returns>
            public DotNetRuntimeStatsCollector StartCollecting(IMetrics metrics)
            {
                var runtimeStatsCollector = new DotNetRuntimeStatsCollector(StatsCollectors.Select(sc => sc(metrics)).ToImmutableHashSet(), _errorHandler, _debugMetrics, metrics);
                runtimeStatsCollector.RegisterMetrics();
                return runtimeStatsCollector;
            }

            /// <summary>
            /// Include metrics around the volume of work scheduled on the worker thread pool
            /// and the scheduling delays.
            /// </summary>
            public Builder WithThreadPoolSchedulingStats()
            {
                StatsCollectors.Add(metrics => new ThreadPoolSchedulingStatsCollector(metrics));
                return this;
            }

            /// <summary>
            /// Include metrics around the size of the worker and IO thread pools and reasons
            /// for worker thread pool changes.
            /// </summary>
            public Builder WithThreadPoolStats()
            {
                StatsCollectors.Add(metrics => new ThreadPoolStatsCollector(metrics));
                return this;
            }

            /// <summary>
            /// Include metrics around volume of locks contended.
            /// </summary>
            public Builder WithContentionStats()
            {
                StatsCollectors.Add(metrics => new ContentionStatsCollector(metrics));
                return this;
            }

            /// <summary>
            /// Include metrics summarizing the volume of methods being compiled
            /// by the Just-In-Time compiler.
            /// </summary>
            public Builder WithJitStats()
            {
                StatsCollectors.Add(metrics => new JitStatsCollector(metrics));
                return this;
            }

            /// <summary>
            /// Include metrics recording the frequency and duration of garbage collections/ pauses, heap sizes and
            /// volume of allocations.
            /// </summary>
            /// <param name="histogramBuckets">Buckets for the GC collection and pause histograms</param>
            public Builder WithGcStats(double[] histogramBuckets = null)
            {
                StatsCollectors.Add(metrics => new GcStatsCollector(metrics));
                return this;
            }

            public Builder WithCustomCollector(IEventSourceStatsCollector statsCollector)
            {
                StatsCollectors.Add(_ => statsCollector);
                return this;
            }

            /// <summary>
            /// Specifies a function to call when an exception occurs within the .NET stats collectors.
            /// Only one error handler may be specified.
            /// </summary>
            /// <param name="handler"></param>
            /// <returns></returns>
            public Builder WithErrorHandler(Action<Exception> handler)
            {
                _errorHandler = handler;
                return this;
            }

            /// <summary>
            /// Include additional debugging metrics. Should NOT be used in production unless debugging
            /// perf issues.
            /// </summary>
            /// <remarks>
            /// Enabling debugging will emit two metrics:
            /// 1. dotnet_debug_events_total - tracks the volume of events being processed by each stats collectorC
            /// 2. dotnet_debug_cpu_seconds_total - tracks (roughly) the amount of CPU consumed by each stats collector.
            /// </remarks>
            /// <param name="generateDebugMetrics"></param>
            /// <returns></returns>
            public Builder WithDebuggingMetrics(bool generateDebugMetrics)
            {
                _debugMetrics = generateDebugMetrics;
                return this;
            }
        }
    }
}
