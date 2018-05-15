using System;
using System.Diagnostics;

namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy
{
    public class StopWatchWrapper : ITimer
    {
        private Stopwatch _watch;
        public void Start()
        {
            if (_watch == null)
            {
                _watch = Stopwatch.StartNew();
            }
        }

        public long ElapsedMilliseconds
        {
            get => _watch.ElapsedMilliseconds;
            set => throw new NotSupportedException();
        }
    }
}