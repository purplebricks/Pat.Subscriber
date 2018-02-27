using System;
using System.Linq;
using System.Threading.Tasks;
using Nito.Collections;
using PB.ITOps.Messaging.PatLite.BatchProcessing;

namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy
{
    public class RateLimiterBatchProcessingBehaviour : IBatchProcessingBehaviour
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
        private readonly Object _rateLock = new Object();

        public Func<Exception, bool> ShouldIncrementProcessingRate { get; set; }

        public void MessageCompleted()
        {
            IncrementProcessingRate();
        }

        private void IncrementProcessingRate()
        {
            var currentInterval = _timer.ElapsedMilliseconds / _intervalInMilliSeconds;
            var intervalPerformance = _messagesProcessed.Count > 0 ? _messagesProcessed[0] : null;
            if (intervalPerformance != null && intervalPerformance.IntervalNumber == currentInterval)
            {
                intervalPerformance.MessagesProcessed += 1;
            }
            else
            {
                lock (_rateLock)
                {
                    if (intervalPerformance != null && intervalPerformance.IntervalNumber == currentInterval)
                    {
                        intervalPerformance.MessagesProcessed += 1;
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
                            MessagesProcessed = 1
                        });
                    }
                }
            }
        }

        public void MessageFailed(Exception ex)
        {
            if (ShouldIncrementProcessingRate(ex))
            {
                IncrementProcessingRate();
            }
        }

        public RateLimiterBatchProcessingBehaviour(RateLimiterPolicyOptions options, SubscriberConfiguration configuration)
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
            ShouldIncrementProcessingRate = options.ShouldIncrementProcessingRate;
        }

        protected virtual void OnThrottling(EventArgs e)
        {
            Throttling?.Invoke(this, e);
        }

        public async Task Invoke(Func<BatchContext, Task> next, BatchContext context)
        {
            _timer.Start();

            await next(context);

            var currentInterval = _timer.ElapsedMilliseconds / _intervalInMilliSeconds;

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
        }
    }
}
