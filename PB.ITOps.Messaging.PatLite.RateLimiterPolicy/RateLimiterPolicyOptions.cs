namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy
{
    public class RateLimiterPolicyOptions
    {
        public IThrottler Throttler { get; set; } = new DefaultThrottler();
        public ITimer Timer { get; set; } = new StopWatchWrapper();
        public RateLimiterPolicyConfiguration PolicyConfiguration { get; set; }

        public RateLimiterPolicyOptions(RateLimiterPolicyConfiguration policyConfiguration)
        {
            PolicyConfiguration = policyConfiguration;
        }
    }
}
