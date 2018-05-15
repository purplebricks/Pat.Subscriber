using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Pat.Subscriber.BatchProcessing;
using Xunit;

namespace Pat.Subscriber.RateLimiterPolicy.UnitTests
{
    public class RateLimiterBehaviourTests
    {
        private readonly TestTimer _timer;
        private readonly IThrottler _throttler;
        private readonly RateLimiterBuilder _behaviourBuilder;

        public RateLimiterBehaviourTests()
        {
            _timer = new TestTimer();
            _throttler = Substitute.For<IThrottler>();
            _behaviourBuilder = new RateLimiterBuilder(_timer, _throttler, new SubscriberConfiguration());
        }

        private Func<Task> BatchProcessedInSeconds(RateLimiterBatchProcessingBehaviour behaviour, int batchSize, int elapsedSeconds)
        {
            return () => {
                _timer.AddElapsedSeconds(elapsedSeconds);

                for (int i = 0; i < batchSize; i++)
                {
                    behaviour.MessageCompleted();
                }

                return Task.CompletedTask;
            };
        }

        [Fact]
        public async Task WhenRateLimitIs1_AndBatchSizeIs1_AndBatchIsProcessedIn10Seconds_ThenDelayFor50Seconds()
        {
            var batchSize = 1;
            var behaviour = _behaviourBuilder.WithRateLimitPerMinute(1).Build();
            var batchProcessedIn10Seconds = BatchProcessedInSeconds(behaviour, batchSize, 10);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await behaviour.Invoke(context => context.Action(),
                    new BatchContext {Action = batchProcessedIn10Seconds, TokenSource = cancellationTokenSource});
            }

            await _throttler.Received(1).Delay(Arg.Is<long>(50000));
        }

        [Fact]
        public async Task WhenRateLimitIs1_AndBatchSizeIs1_AndBatchIsProcessedIn60Seconds_NoDelay()
        {
            var batchSize = 1;
            var behaviour = _behaviourBuilder.WithRateLimitPerMinute(batchSize).Build();
            var batchProcessedIn60Seconds = BatchProcessedInSeconds(behaviour, batchSize, 60);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await behaviour.Invoke(context => context.Action(),
                    new BatchContext { Action = batchProcessedIn60Seconds, TokenSource = cancellationTokenSource });
            }

            await _throttler.DidNotReceiveWithAnyArgs().Delay(0);
        }

        [Fact]
        public async Task WhenRateLimitIs2_AndBatchSizeIs2_AndBatchIsProcessedIn10Seconds_ThenDelayFor50Seconds()
        {
            var batchSize = 2;
            var behaviour = _behaviourBuilder.WithRateLimitPerMinute(2).Build();
            var batchProcessedIn10Seconds = BatchProcessedInSeconds(behaviour, batchSize, 10);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await behaviour.Invoke(context => context.Action(),
                    new BatchContext { Action = batchProcessedIn10Seconds, TokenSource = cancellationTokenSource });
            }

            await _throttler.Received(1).Delay(Arg.Is<long>(50000));
        }

        [Fact]
        public async Task WhenRateLimitIs2_AndBatchSizeIs2_AndBatchIsProcessedIn30Seconds_ThenDelayFor30Seconds()
        {
            var batchSize = 2;
            var behaviour = _behaviourBuilder.WithRateLimitPerMinute(2).Build();
            var batchProcessedIn30Seconds = BatchProcessedInSeconds(behaviour, batchSize, 30);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await behaviour.Invoke(context => context.Action(),
                    new BatchContext { Action = batchProcessedIn30Seconds, TokenSource = cancellationTokenSource });
            }

            await _throttler.Received(1).Delay(Arg.Is<long>(30000));
        }

        [Fact]
        public async Task WhenRateLimitIs12_AndBatchSizeIs2_AndBatchIsProcessedIn10Seconds_ThenNoDelay()
        {
            var batchSize = 2;
            var behaviour = _behaviourBuilder.WithRateLimitPerMinute(12).Build();
            var batchProcessedIn10Seconds = BatchProcessedInSeconds(behaviour, batchSize, 10);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await behaviour.Invoke(context => context.Action(),
                    new BatchContext { Action = batchProcessedIn10Seconds, TokenSource = cancellationTokenSource });
            }

            await _throttler.DidNotReceiveWithAnyArgs().Delay(0);
        }

        [Fact]
        public async Task WhenRateLimitIs12_AndBatchSizeIs2_FirstBatchProcessedIn10SecondsSecondBatchProcessedIn5Seconds_ThenDelay5Seconds()
        {
            var batchSize = 2;
            var behaviour = _behaviourBuilder.WithRateLimitPerMinute(12).Build();
            var batchProcessedIn10Seconds = BatchProcessedInSeconds(behaviour, batchSize, 10);
            var batchProcessedIn5Seconds = BatchProcessedInSeconds(behaviour, batchSize, 5);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await behaviour.Invoke(context => context.Action(),
                    new BatchContext { Action = batchProcessedIn10Seconds, TokenSource = cancellationTokenSource });
                await behaviour.Invoke(context => context.Action(),
                    new BatchContext { Action = batchProcessedIn5Seconds, TokenSource = cancellationTokenSource });
            }

            await _throttler.Received(1).Delay(Arg.Is<long>(5000));
        }

        [Fact]
        public async Task WhenIdleFor3Minutes_And60MessagesProcessedInAMinute_AndRateIs15PerMinute_AndRollingIntervalIs4Minutes_DelayNotCalled()
        {
            var batchSize = 1;
            var behaviour = _behaviourBuilder
                .WithGroupingIntervalInMilliseconds(1000 * 60)//1 minute
                .WithRollingIntervals(4)//consider 4 previous intervals when calculating rate
                .WithRateLimitPerMinute(15).Build();

            var batchProcessedIn10Seconds = BatchProcessedInSeconds(behaviour, batchSize, 10);
            var batchProcessedIn1Second = BatchProcessedInSeconds(behaviour, batchSize, 1);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await behaviour.Invoke(context => context.Action(),
                    new BatchContext { Action = batchProcessedIn10Seconds, TokenSource = cancellationTokenSource });
            
                _timer.AddElapsedSeconds(290);

                //simulate processing 60 messages in a minute
                for (int i = 0; i < 60; i++)
                {
                    await behaviour.Invoke(context => context.Action(),
                        new BatchContext { Action = batchProcessedIn1Second, TokenSource = cancellationTokenSource });
                }
            }

            await _throttler.DidNotReceiveWithAnyArgs().Delay(0);
        }

        [Fact]
        public async Task WhenIdleFor3Minutes_And60MessagesProcessedInAMinute_AndRateIs15PerMinute_AndRollingIntervalIs3Minutes_ThenDelayFor60Seconds()
        {
            var batchSize = 1;
            var behaviour = _behaviourBuilder
                .WithGroupingIntervalInMilliseconds(1000 * 60)//1 minute
                .WithRollingIntervals(3)//consider 3 previous intervals when calculating rate
                .WithRateLimitPerMinute(15).Build();

            var batchProcessedIn10Seconds = BatchProcessedInSeconds(behaviour, batchSize, 10);
            var batchProcessedIn1Second = BatchProcessedInSeconds(behaviour, batchSize, 1);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await behaviour.Invoke(context => context.Action(),
                    new BatchContext { Action = batchProcessedIn10Seconds, TokenSource = cancellationTokenSource });
                _timer.AddElapsedSeconds(290);

                //simulate processing 60 messages in a minute
                for (int i = 0; i < 60; i++)
                {
                    await behaviour.Invoke(context => context.Action(),
                        new BatchContext { Action = batchProcessedIn1Second, TokenSource = cancellationTokenSource });
                }
            }

            await _throttler.Received(1).Delay(Arg.Is<long>(60000));
        }

        [Fact]
        public async Task WhenRateLimitIs2_AndBatchSizeIs4_AndBatchIsProcessedIn30Sec_ThenDelayFor90Secs()
        {
            var batchSize = 4;
            var behaviour = _behaviourBuilder
                .WithRateLimitPerMinute(2).Build();

            var batchProcessedIn30Seconds = BatchProcessedInSeconds(behaviour, batchSize, 30);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await behaviour.Invoke(context => context.Action(),
                    new BatchContext { Action = batchProcessedIn30Seconds, TokenSource = cancellationTokenSource });
            }

            await _throttler.Received(1).Delay(Arg.Is<long>(90000));
        }
    }
}