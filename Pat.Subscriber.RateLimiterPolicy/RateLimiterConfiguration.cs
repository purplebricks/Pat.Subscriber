namespace Pat.Subscriber.RateLimiterPolicy
{
    public class RateLimiterConfiguration
    {
        /// <summary>
        /// Rate limit in number of messages per minute
        /// </summary>
        public int RateLimit { get; set; }

        /// <summary>
        /// Resolution at which performance statistics used for rate limtting are aggregated
        /// </summary>
        public int IntervalInMilliSeconds { get; set; } = 5000;

        /// <summary>
        /// The number of historic intervals over which average hit rates will be calculated and maintained in relation to the rate limit
        /// </summary>
        public int RollingIntervals { get; set; } = 12;
    }
}
