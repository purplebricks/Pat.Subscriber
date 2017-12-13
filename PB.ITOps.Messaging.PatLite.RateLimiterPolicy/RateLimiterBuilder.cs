namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy
{
    public class RateLimiterBuilder
    {
        private int _rateLimit = 1;
        private int _groupingInterval = 1000 * 60; //1 minute
        private int _rollingIntervals = 4;

        private readonly ITimer _timer;
        private readonly IThrottler _throttler;
        private readonly SubscriberConfiguration _subscriberConfiguration;

        public RateLimiterBuilder(ITimer timer, IThrottler throttler, SubscriberConfiguration subscriberConfiguration)
        {
            _timer = timer;
            _throttler = throttler;
            _subscriberConfiguration = subscriberConfiguration;
        }

        public RateLimiterBuilder WithRateLimitPerMinute(int value)
        {
            _rateLimit = value;
            return this;
        }

        public RateLimiterBuilder WithGroupingIntervalInMilliseconds(int value)
        {
            _groupingInterval = value;
            return this;
        }

        public RateLimiterBuilder WithRollingIntervals(int value)
        {
            _rollingIntervals = value;
            return this;
        }

        public RateLimiterBehaviour Build()
        {
            return new RateLimiterBehaviour(new RateLimiterPolicyOptions(
                new RateLimiterConfiguration
                {
                    IntervalInMilliSeconds = _groupingInterval,
                    RollingIntervals = _rollingIntervals,
                    RateLimit = _rateLimit
                })
            {
                Timer = _timer,
                Throttler = _throttler
            }, _subscriberConfiguration);
        }
    }
}
