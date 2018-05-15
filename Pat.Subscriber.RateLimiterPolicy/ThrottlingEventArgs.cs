using System;

namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy
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
