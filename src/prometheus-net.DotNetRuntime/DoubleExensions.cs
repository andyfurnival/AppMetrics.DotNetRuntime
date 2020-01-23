using System;

namespace Prometheus.DotNetRuntime
{
    public static class DoubleExensions
    {
        public static long RoundToLong(this double value)
        {
            return (long) Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}