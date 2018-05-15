using System;

namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy
{
    public class RateLimiterPolicyConfigurationException : Exception
    {
        public RateLimiterPolicyConfigurationException(string message): base(message)
        {
            
        }
    }
}