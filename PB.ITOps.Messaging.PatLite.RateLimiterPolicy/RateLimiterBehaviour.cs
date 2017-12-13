using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nito.Collections;
using PB.ITOps.Messaging.PatLite.BatchProcessing;

namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy
{
    public class RateLimiterBehaviour : IBatchProcessingBehaviour
    {
        internal class IntervalPerformance
        {
            public long IntervalNumber { get; set; }
            public int MessagesProcessed { get; set; }
        }

        private readonly int _rateLimit;
        private readonly ITimer _timer;
        private readonly IThrottler _throttler;
        private readonly Deque<IntervalPerformance> _messagesProcessed;
        private readonly int _intervalInMilliSeconds;
        private readonly int _rollingIntervals;
        private readonly double _intervalsPerMinute;
        public delegate void ThrottlingHandler(object sender, EventArgs e);
        public event ThrottlingHandler Throttling;

        public RateLimiterBehaviour(RateLimiterPolicyOptions options, SubscriberConfiguration configuration)
        {
            if (configuration.ConcurrentBatches > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(configuration.ConcurrentBatches), "Rate Limiter does not support concurrent batches.");
            }

            _timer = options.Timer;
            _throttler = options.Throttler;
            _rateLimit = options.Configuration.RateLimit;
            _messagesProcessed = new Deque<IntervalPerformance>(options.Configuration.RollingIntervals + 1);
            _intervalInMilliSeconds = options.Configuration.IntervalInMilliSeconds;
            _intervalsPerMinute = (double)1000 * 60 / options.Configuration.IntervalInMilliSeconds;
            _rollingIntervals = options.Configuration.RollingIntervals;
        }

        protected virtual void OnThrottling(EventArgs e)
        {
            Throttling?.Invoke(this, e);
        }

        public async Task<int> Invoke(Func<BatchContext, Task<int>> next, BatchContext context)
        {
            _timer.Start();

            var messagesProcessed = await next(context);

            if (messagesProcessed > _rateLimit)
            {
                throw new RateLimiterPolicyConfigurationException($"Invalid rate limit: message batch size processed was {messagesProcessed}, which exceeds the rate limit of {_rateLimit}");
            }

            var currentInterval = _timer.ElapsedMilliseconds / _intervalInMilliSeconds;
            var intervalPerformance = _messagesProcessed.Count > 0 ? _messagesProcessed[0] : null;
            if (intervalPerformance != null && intervalPerformance.IntervalNumber == currentInterval)
            {
                intervalPerformance.MessagesProcessed += messagesProcessed;
            }
            else
            {
                if (_messagesProcessed.Count > _rollingIntervals)
                {
                    _messagesProcessed.RemoveFromBack();
                }
                _messagesProcessed.AddToFront(new IntervalPerformance
                {
                    IntervalNumber = currentInterval,
                    MessagesProcessed = messagesProcessed
                });
            }

            var startInterval = (currentInterval - _rollingIntervals > 0) ? currentInterval - _rollingIntervals : 0;
            var messagesProcessedInLastInterval = 0;
            for (long i = startInterval; i <= currentInterval; i++)
            {
                var intervalFound = _messagesProcessed.FirstOrDefault(x => x.IntervalNumber == i);
                if (intervalFound != null)
                {
                    messagesProcessedInLastInterval += intervalFound.MessagesProcessed;
                }
            }

            var elapsedSinceLastIntervalBegan = _timer.ElapsedMilliseconds - currentInterval * _intervalInMilliSeconds;
            var wholeIntervalsElapsed = currentInterval - startInterval;

            var elapsedTime = wholeIntervalsElapsed * _intervalInMilliSeconds + elapsedSinceLastIntervalBegan;
            double elapsedIntervals = (double)elapsedTime / (_intervalInMilliSeconds);
            var processingRate = _intervalsPerMinute * messagesProcessedInLastInterval / elapsedIntervals;
            if (processingRate > _rateLimit)
            {
                var targetTimePerMessage = 1000 * 60 / _rateLimit;
                var targetTime = messagesProcessedInLastInterval * targetTimePerMessage;
                var delay = targetTime - elapsedTime;
                OnThrottling(new ThrottlingEventArgs(delay));
                await _throttler.Delay(delay);
            }

            return messagesProcessed;
        }
    }
}
