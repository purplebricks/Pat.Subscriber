using System;
using System.Threading.Tasks;

namespace Pat.Subscriber.RateLimiterPolicy
{
    public class DefaultThrottler : IThrottler
    {
        public async Task Delay(long milliseconds)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds)).ConfigureAwait(false);
        }
    }
}