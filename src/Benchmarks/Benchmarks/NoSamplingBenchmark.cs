using Prometheus.DotNetRuntime;

namespace Benchmarks.Benchmarks
{
    public class NoSamplingBenchmark : DotNetRuntimeStatsBenchmarkBase
    {
        protected override DotNetRuntimeStatsBuilder.Builder GetStatsBuilder()
        {
            return DotNetRuntimeStatsBuilder.Default().WithThreadPoolSchedulingStats(sampleRate: SampleEvery.OneEvent);
        }
    }
}