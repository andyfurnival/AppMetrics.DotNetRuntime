using System;
using System.Runtime;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.AspNetCore;
using App.Metrics.Counter;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AppMetrics.DotNetRuntime;

namespace AspNetCoreExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            

            CreateWebHostBuilder(args)
                .ConfigureMetricsWithDefaults(m =>
                    {
                        m.Report.ToGraphite($"net.tcp://localhost:2003", TimeSpan.FromSeconds(10));
                    })
                .UseMetrics()
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureKestrel(opts =>
                {
                    opts.AllowSynchronousIO = true;
                })
                .UseStartup<Startup>();
    }
}
