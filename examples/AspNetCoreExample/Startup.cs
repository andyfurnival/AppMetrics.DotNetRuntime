using System;
using App.Metrics;
using App.Metrics.Counter;
using AppMetrics.DotNetRuntime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreExample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            var metrics = app.ApplicationServices.GetService<IMetrics>();
            metrics.Measure.Counter.Increment(new CounterOptions(){Name = "test-counter", Context = "some-context"});
            if (Environment.GetEnvironmentVariable("NOMON") == null)
            {
                Console.WriteLine("Enabling prometheus-net.DotNetStats...");

                DotNetRuntimeStatsBuilder.Customize(metrics)
                    .WithThreadPoolSchedulingStats()
                    .WithContentionStats()
                    .WithGcStats()
                    .WithJitStats()
                    .WithThreadPoolStats()
                    .WithErrorHandler(ex => Console.WriteLine("ERROR: " + ex.ToString()))
                    .WithDebuggingMetrics(true)
                    .StartCollecting();
            }

            app.UseHttpsRedirection();
            app.UseRouting(); 
            
            app.UseEndpoints(endpoints =>
            {
                // Mapping of endpoints goes here:
                endpoints.MapControllers();
            });
        }
    }
}