using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using App.Metrics;
using App.Metrics.Registry;
using Prometheus.DotNetRuntime.StatsCollectors;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime
{
    /// <summary>
    /// Configures what .NET core runtime metrics will be collected. 
    /// </summary>
    public static class DotNetRuntimeStatsBuilder
    {
        /// <summary>
        /// Includes all available .NET runtime metrics by default. Call <see cref="Builder.StartCollecting()"/>
        /// to begin collecting metrics.
        /// </summary>
        /// <returns></returns>
        public static Builder Default(IMetrics metrics)
        {
            return Customize(metrics)
                .WithContentionStats()
                .WithJitStats()
                .WithThreadPoolSchedulingStats()
                .WithThreadPoolStats()
                .WithGcStats();
        }

        /// <summary>
        /// Allows you to customize the types of metrics collected. 
        /// </summary>
        /// <param name="metrics"></param>
        /// <returns></returns>
        /// <remarks>
        /// Include specific .NET runtime metrics by calling the WithXXX() methods and then call <see cref="Builder.StartCollecting()"/>
        /// </remarks>
        public static Builder Customize(IMetrics metrics)
        {
            return new Builder(metrics);
        }

        public class Builder
        {
            private readonly IMetrics _metrics;
            private Action<Exception> _errorHandler;
            private bool _debugMetrics;

            public Builder(IMetrics metrics)
            {
                _metrics = metrics;
            }

            internal HashSet<IEventSourceStatsCollector> StatsCollectors { get; } = new HashSet<IEventSourceStatsCollector>(new TypeEquality<IEventSourceStatsCollector>());

            /// <summary>
            /// Finishes configuration and starts collecting .NET runtime metrics. Returns a <see cref="IDisposable"/> that
            /// can be disposed of to stop metric collection. 
            /// </summary>
            /// <returns></returns>
            public IDisposable StartCollecting()
            {
                var runtimeStatsCollector = new DotNetRuntimeStatsCollector(StatsCollectors.ToImmutableHashSet(), _errorHandler, _debugMetrics, _metrics);
                runtimeStatsCollector.RegisterMetrics(_metrics);
           // Seif: what to do with this?? doesnt look like we have a similar hook in appmetrics
           // metrics.AddBeforeCollectCallback(runtimeStatsCollector.UpdateMetrics);
                return runtimeStatsCollector;
            }

            /// <summary>
            /// Include metrics around the volume of work scheduled on the worker thread pool
            /// and the scheduling delays.
            /// </summary>
            /// <param name="histogramBuckets">Buckets for the scheduling delay histogram</param>
            /// <param name="sampleRate">
            /// The sampling rate for thread pool scheduling events. A lower sampling rate reduces memory use
            /// but reduces the accuracy of metrics produced (as a percentage of events are discarded).
            /// If your application achieves a high level of throughput (thousands of work items scheduled per second on
            /// the thread pool), it's recommend to reduce the sampling rate even further.
            /// </param>
            public Builder WithThreadPoolSchedulingStats(double[] histogramBuckets = null, SampleEvery sampleRate = SampleEvery.TenEvents)
            {
                StatsCollectors.Add(new ThreadPoolSchedulingStatsCollector(_metrics));
                return this;
            }

            /// <summary>
            /// Include metrics around the size of the worker and IO thread pools and reasons
            /// for worker thread pool changes.
            /// </summary>
            public Builder WithThreadPoolStats()
            {
                StatsCollectors.Add(new ThreadPoolStatsCollector(_metrics));
                return this;
            }

            /// <summary>
            /// Include metrics around volume of locks contended.
            /// </summary>
            /// <param name="sampleRate">
            /// The sampling rate for contention events (defaults to 100%). A lower sampling rate reduces memory use
            /// but reduces the accuracy of metrics produced (as a percentage of events are discarded).
            /// </param>
            public Builder WithContentionStats(SampleEvery sampleRate = SampleEvery.TwoEvents)
            {
                StatsCollectors.Add(new ContentionStatsCollector(_metrics));
                return this;
            }

            /// <summary>
            /// Include metrics summarizing the volume of methods being compiled
            /// by the Just-In-Time compiler.
            /// </summary>
            /// <param name="sampleRate">
            /// The sampling rate for JIT events. A lower sampling rate reduces memory use
            /// but reduces the accuracy of metrics produced (as a percentage of events are discarded).
            /// If your application achieves a high level of throughput (thousands of work items scheduled per second on
            /// the thread pool), it's recommend to reduce the sampling rate even further.
            /// </param>
            public Builder WithJitStats(SampleEvery sampleRate = SampleEvery.TenEvents)
            {
                StatsCollectors.Add(new JitStatsCollector(_metrics));
                return this;
            }

            /// <summary>
            /// Include metrics recording the frequency and duration of garbage collections/ pauses, heap sizes and
            /// volume of allocations.
            /// </summary>
            /// <param name="histogramBuckets">Buckets for the GC collection and pause histograms</param>
            public Builder WithGcStats(double[] histogramBuckets = null)
            {
                StatsCollectors.Add(new GcStatsCollector(histogramBuckets ?? Constants.DefaultHistogramBuckets, _metrics));
                return this;
            }

            public Builder WithCustomCollector(IEventSourceStatsCollector statsCollector)
            {
                StatsCollectors.Add(statsCollector);
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

            internal class TypeEquality<T> : IEqualityComparer<T>
            {
                public bool Equals(T x, T y)
                {
                    return x.GetType() == y.GetType();
                }

                public int GetHashCode(T obj)
                {
                    return obj.GetType().GetHashCode();
                }
            }
        }
    }
}