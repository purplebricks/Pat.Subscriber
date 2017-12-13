namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy
{
    public class RateLimiterPolicyOptions
    {
        public IThrottler Throttler { get; set; } = new DefaultThrottler();
        public ITimer Timer { get; set; } = new StopWatchWrapper();
        public RateLimiterConfiguration Configuration { get; set; }

        public RateLimiterPolicyOptions(RateLimiterConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}
