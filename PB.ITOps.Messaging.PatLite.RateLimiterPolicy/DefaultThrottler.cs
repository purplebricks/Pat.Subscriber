using System;
using System.Threading.Tasks;

namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy
{
    public class DefaultThrottler : IThrottler
    {
        public async Task Delay(long milliseconds)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds));
        }
    }
}