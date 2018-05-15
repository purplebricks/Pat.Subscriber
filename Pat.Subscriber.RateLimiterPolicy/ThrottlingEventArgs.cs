using System;

namespace Pat.Subscriber.RateLimiterPolicy
{
    public class ThrottlingEventArgs : EventArgs
    {
        public long Delay { get; }

        public ThrottlingEventArgs(long delay)
        {
            Delay = delay;
        }
    }
}
