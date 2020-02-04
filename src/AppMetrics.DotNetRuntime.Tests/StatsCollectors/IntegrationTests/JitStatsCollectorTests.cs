using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using NUnit.Framework;
using AppMetrics.DotNetRuntime;
using AppMetrics.DotNetRuntime.StatsCollectors;


namespace AppMetrics.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    internal class Given_A_JitStatsCollector_That_Samples_Every_Jit_Event : StatsCollectorIntegrationTestBase<JitStatsCollector>
    {
        protected override JitStatsCollector CreateStatsCollector()
        {
            return new JitStatsCollector(MetricsClient);
        }

        [Test]
        public void When_a_method_is_jitted_then_its_compilation_is_measured()
        {
            // arrange
            MetricsClient.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal, new MetricTags("dynamic", "true")).Reset();
            Task.WaitAll((MetricsClient.ReportRunner.RunAllAsync().ToArray()));
            
            Thread.Sleep(100);
            var previousMethodJitted =
                GetTimer(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal.Name, "dynamic:false").Value.Rate.Count;
            var previousMethodJittedMilliSeconds =
                GetTimer(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal.Name, "dynamic:false").Value.Histogram.Sum;
            
            // act (call a method, JIT'ing it)
            ToJit();
            
            // assert
            Task.WaitAll((MetricsClient.ReportRunner.RunAllAsync().ToArray()));
            Thread.Sleep(100);
            Assert.That(() => GetTimer(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal.Name, "dynamic:false").Value.Rate.Count, Is.GreaterThanOrEqualTo(previousMethodJitted  + 1).After(100, 10));
            Assert.That(GetTimer(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal.Name, "dynamic:false").Value.Histogram.Sum, Is.GreaterThan(previousMethodJittedMilliSeconds));
        }


        [Test]
        public void When_a_method_is_jitted_then_the_CPU_ratio_can_be_measured()
        {
            MetricsClient.Manage.Reset();
            // act (call a method, JIT'ing it)
            ToJit();
            
            // assert
            Task.WaitAll((MetricsClient.ReportRunner.RunAllAsync().ToArray()));
            Thread.Sleep(500);
            Assert.That(() => GetGauage(DotNetRuntimeMetricsRegistry.Gauges.CpuRatio.Name).Value, Is.GreaterThanOrEqualTo(0.0).After(100, 10));
        }
        
        [Test]
        public void When_a_dynamic_method_is_jitted_then_its_compilation_is_measured()
        {
            
            // arrange
            MetricsClient.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal, new MetricTags("dynamic", "true")).Reset();
            Task.WaitAll((MetricsClient.ReportRunner.RunAllAsync().ToArray()));
            
            var previousMethodJitted =
                GetTimer(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal.Name, "dynamic:true").Value.Rate.Count;
            var previousMethodJittedMilliSeconds =
                GetTimer(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal.Name, "dynamic:true").Value.Histogram.Sum;
            
            // act (call a method, JIT'ing it)
            ToJitDynamic();
            
            // assert
            Task.WaitAll((MetricsClient.ReportRunner.RunAllAsync().ToArray()));
            
            Assert.That(() => GetTimer(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal.Name, "dynamic:true").Value.Rate.Count, Is.GreaterThanOrEqualTo(previousMethodJitted  + 1).After(100, 10));
            Assert.That(GetTimer(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal.Name, "dynamic:true").Value.Histogram.Sum, Is.GreaterThan(previousMethodJittedMilliSeconds));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int ToJit()
        {
            return 1;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private int ToJitDynamic()
        {
            dynamic o = "string";
            return o.Length;
        }
    }
    
    internal class Given_A_JitStatsCollector_That_Samples_Every_Fifth_Jit_Event : StatsCollectorIntegrationTestBase<JitStatsCollector>
    {
        protected override JitStatsCollector CreateStatsCollector()
        {
            return new JitStatsCollector(MetricsClient);
        }

        [Test]
        public void When_many_methods_are_jitted_then_their_compilation_is_measured()
        {
            MetricsClient.Provider.Timer.Instance(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal, new MetricTags("dynamic", "true")).Reset();
            // arrange
            Task.WaitAll((MetricsClient.ReportRunner.RunAllAsync().ToArray()));
            
            Thread.Sleep(100);
            var previousMethodJitted =
                GetTimer(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal.Name, "dynamic:true").Value.Rate.Count;
            var previousMethodJittedMilliSeconds =
                GetTimer(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal.Name, "dynamic:true").Value.Histogram.Sum;
            
            // act
            var sp = Stopwatch.StartNew();
            Compile100Methods(() => 1);
            sp.Stop();
            
            Task.WaitAll((MetricsClient.ReportRunner.RunAllAsync().ToArray()));
            
            // assert
            Assert.That(() => GetTimer(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal.Name, "dynamic:true").Value.Rate.Count, Is.GreaterThanOrEqualTo(previousMethodJitted  + 20).After(100, 10));
            Assert.That(GetTimer(DotNetRuntimeMetricsRegistry.Timers.MethodsJittedMilliSecondsTotal.Name, "dynamic:true").Value.Histogram.Sum, Is.GreaterThan(previousMethodJittedMilliSeconds + sp.Elapsed.TotalMilliseconds).Within(100.0));
        }

        private void Compile100Methods(Expression<Func<int>> toCompile)
        {
            for (int i = 0; i < 100; i++)
            {
                toCompile.Compile();
            }
        }
    }
}