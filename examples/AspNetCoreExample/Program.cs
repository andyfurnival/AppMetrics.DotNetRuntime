using System;
using App.Metrics;
using App.Metrics.AspNetCore;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

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
