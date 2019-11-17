using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Prometheus.DotNetRuntime;

namespace Benchmarks.Benchmarks
{
    public class BaselineBenchmark : AspNetBenchmarkBase
    {
        [Benchmark(Baseline = true, Description = "Bombard ASP.NET core server with requests with all prometheus-net.DotNetRuntime monitoring disabled.")]
        public async Task Make_Requests()
        {
            await MakeHttpRequests();
        }
    }
}