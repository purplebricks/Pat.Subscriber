using System;

namespace Pat.Subscriber.RateLimiterPolicy
{
    public class RateLimiterPolicyConfigurationException : Exception
    {
        public RateLimiterPolicyConfigurationException(string message): base(message)
        {
            
        }
    }
}