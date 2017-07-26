using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using PB.ITOps.Messaging.PatLite.RateLimiterPolicy;
using Xunit;

namespace PB.ITOps.Messaging.PatLite.UnitTests.Policies
{
    public class TestTimer : ITimer
    {
        private long _elapsedTime = -1;

        public void AddElapsedSeconds(int value)
        {
            _elapsedTime += value * 1000;
        }

        public void AddElapsedMilliseconds(int value)
        {
            _elapsedTime += value;
        }

        public void Start()
        {
            if (_elapsedTime == -1)
            {
                _elapsedTime = 0;
            }
        }

        public long ElapsedMilliseconds
        {
            get => _elapsedTime; set => _elapsedTime = value;
        }
    }

    
    public class RateLimiterPolicyTests
    {
        private readonly TestTimer _timer;
        private readonly IThrottler _throttler;
        private readonly RateLimiterBuilder _policyBuilder;

        public RateLimiterPolicyTests()
        {
            _timer = new TestTimer();
            _throttler = Substitute.For<IThrottler>();
            _policyBuilder = new RateLimiterBuilder(_timer, _throttler);
        }

        private Func<Task<int>> BatchProcessedInSeconds(int batchSize, int elapsedSeconds)
        {
            return () => {
                _timer.AddElapsedSeconds(elapsedSeconds);
                return Task.FromResult(batchSize);
            };
        }


        [Fact]
        public async Task WhenRateLimitIs1_AndBatchSizeIs1_AndBatchIsProcessedIn10Seconds_ThenDelayFor50Seconds()
        {
            var batchSize = 1;
            var policy = _policyBuilder.WithRateLimitPerMinute(1).Build();
            var batchProcessedIn10Seconds = BatchProcessedInSeconds(batchSize, 10);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await policy.ProcessMessageBatch(batchProcessedIn10Seconds, cancellationTokenSource);
            }

            await _throttler.Received(1).Delay(Arg.Is<long>(50000));
        }

        [Fact]
        public async Task WhenRateLimitIs1_AndBatchSizeIs1_AndBatchIsProcessedIn60Seconds_NoDelay()
        {
            var batchSize = 1;
            var policy = _policyBuilder.WithRateLimitPerMinute(batchSize).Build();
            var batchProcessedIn60Seconds = BatchProcessedInSeconds(batchSize, 60);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await policy.ProcessMessageBatch(batchProcessedIn60Seconds, cancellationTokenSource);
            }

            await _throttler.DidNotReceiveWithAnyArgs().Delay(0);
        }

        [Fact]
        public async Task WhenRateLimitIs2_AndBatchSizeIs2_AndBatchIsProcessedIn10Seconds_ThenDelayFor50Seconds()
        {
            var batchSize = 2;
            var policy = _policyBuilder.WithRateLimitPerMinute(2).Build();
            var batchProcessedIn10Seconds = BatchProcessedInSeconds(batchSize, 10);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await policy.ProcessMessageBatch(batchProcessedIn10Seconds, cancellationTokenSource);
            }

            await _throttler.Received(1).Delay(Arg.Is<long>(50000));
        }

        [Fact]
        public async Task WhenRateLimitIs2_AndBatchSizeIs2_AndBatchIsProcessedIn30Seconds_ThenDelayFor30Seconds()
        {
            var batchSize = 2;
            var policy = _policyBuilder.WithRateLimitPerMinute(2).Build();
            var batchProcessedIn30Seconds = BatchProcessedInSeconds(batchSize, 30);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await policy.ProcessMessageBatch(batchProcessedIn30Seconds, cancellationTokenSource);
            }

            await _throttler.Received(1).Delay(Arg.Is<long>(30000));
        }

        [Fact]
        public async Task WhenRateLimitIs12_AndBatchSizeIs2_AndBatchIsProcessedIn10Seconds_ThenNoDelay()
        {
            var batchSize = 2;
            var policy = _policyBuilder.WithRateLimitPerMinute(12).Build();
            var batchProcessedIn10Seconds = BatchProcessedInSeconds(batchSize, 10);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await policy.ProcessMessageBatch(batchProcessedIn10Seconds, cancellationTokenSource);
            }

            await _throttler.DidNotReceiveWithAnyArgs().Delay(0);
        }

        [Fact]
        public async Task WhenRateLimitIs12_AndBatchSizeIs2_FirstBatchProcessedIn10SecondsSecondBatchProcessedIn5Seconds_ThenDelay5Seconds()
        {
            var batchSize = 2;
            var policy = _policyBuilder.WithRateLimitPerMinute(12).Build();
            var batchProcessedIn10Seconds = BatchProcessedInSeconds(batchSize, 10);
            var batchProcessedIn5Seconds = BatchProcessedInSeconds(batchSize, 5);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await policy.ProcessMessageBatch(batchProcessedIn10Seconds, cancellationTokenSource);
                await policy.ProcessMessageBatch(batchProcessedIn5Seconds, cancellationTokenSource);
            }

            await _throttler.Received(1).Delay(Arg.Is<long>(5000));
        }

        [Fact]
        public async Task WhenIdleFor3Minutes_And60MessagesProcessedInAMinute_AndRateIs15PerMinute_AndRollingIntervalIs4Minutes_DelayNotCalled()
        {
            var batchSize = 1;
            var policy = _policyBuilder
                .WithGroupingIntervalInMilliseconds(1000*60)//1 minute
                .WithRollingIntervals(4)//consider 4 previous intervals when calculating rate
                .WithRateLimitPerMinute(15).Build();

            var batchProcessedIn10Seconds = BatchProcessedInSeconds(batchSize, 10);
            var batchProcessedIn1Second = BatchProcessedInSeconds(batchSize, 1);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await policy.ProcessMessageBatch(batchProcessedIn10Seconds, cancellationTokenSource);
                _timer.AddElapsedSeconds(290);

                //simulate processing 60 messages in a minute
                for (int i = 0; i < 60; i++)
                {
                    await policy.ProcessMessageBatch(batchProcessedIn1Second, cancellationTokenSource); 
                }
            }

            await _throttler.DidNotReceiveWithAnyArgs().Delay(0);
        }

        [Fact]
        public async Task WhenIdleFor3Minutes_And60MessagesProcessedInAMinute_AndRateIs15PerMinute_AndRollingIntervalIs3Minutes_ThenDelayFor60Seconds()
        {
            var batchSize = 1;
            var policy = _policyBuilder
                .WithGroupingIntervalInMilliseconds(1000 * 60)//1 minute
                .WithRollingIntervals(3)//consider 3 previous intervals when calculating rate
                .WithRateLimitPerMinute(15).Build();

            var batchProcessedIn10Seconds = BatchProcessedInSeconds(batchSize, 10);
            var batchProcessedIn1Second = BatchProcessedInSeconds(batchSize, 1);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await policy.ProcessMessageBatch(batchProcessedIn10Seconds, cancellationTokenSource);
                _timer.AddElapsedSeconds(290);

                //simulate processing 60 messages in a minute
                for (int i = 0; i < 60; i++)
                {
                    await policy.ProcessMessageBatch(batchProcessedIn1Second, cancellationTokenSource);
                }
            }

            await _throttler.Received(1).Delay(Arg.Is<long>(60000));
        }

        [Fact]
        public async Task WhenRateLimitIs2_AndBatchSizeIs4_AndBatchIsProcessedIn30Sec_ThenThrowsConfigException()
        {
            var batchSize = 4;
            var policy = _policyBuilder
                .WithRateLimitPerMinute(2).Build();
            
            var batchProcessedIn30Seconds = new Func<Task<int>>(() => {
                _timer.AddElapsedMilliseconds(30*1000);
                return Task.FromResult(batchSize);
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await Assert.ThrowsAsync<ConfigurationErrorsException>(() => policy.ProcessMessageBatch(batchProcessedIn30Seconds, cancellationTokenSource));
            }
        }
    }

}
