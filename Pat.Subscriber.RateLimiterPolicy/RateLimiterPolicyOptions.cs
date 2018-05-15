using System;

namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy
{
    public class RateLimiterPolicyOptions
    {
        public IThrottler Throttler { get; set; } = new DefaultThrottler();
        public ITimer Timer { get; set; } = new StopWatchWrapper();
        public Func<Exception, bool> ShouldIncrementProcessingRate { get; set; } = exception => false;
        public RateLimiterConfiguration Configuration { get; set; }
        public RateLimiterPolicyOptions(RateLimiterConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}
