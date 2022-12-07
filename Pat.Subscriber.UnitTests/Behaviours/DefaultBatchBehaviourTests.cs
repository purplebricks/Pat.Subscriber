using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Pat.Subscriber.BatchProcessing;
using Xunit;

namespace Pat.Subscriber.UnitTests.Behaviours
{
    public class DefaultBatchBehaviourTests
    {
        private readonly MockLogger<DefaultBatchProcessingBehaviour> _log;
        private readonly BatchProcessingBehaviourPipeline _defaultBehaviour;

        public DefaultBatchBehaviourTests()
        {
            _log = Substitute.For<MockLogger<DefaultBatchProcessingBehaviour>>();
            _defaultBehaviour = new BatchProcessingBehaviourPipeline()
                .AddBehaviour(new DefaultBatchProcessingBehaviour(_log, new SubscriberConfiguration
                {
                    SubscriberName = "test"
                }));
        }

        [Fact]
        public async Task WhenProcessMessageBatchThrows_InfrastructureErrorLogged()
        {
            var ex = new Exception("TEST");
  
            await _defaultBehaviour.Invoke(() => throw ex, new CancellationTokenSource());

            _log.Received(1).Log(
                LogLevel.Error, 
                Arg.Is<string>(m => m.ToString().Contains("Unhandled non transient exception on queue")));
        }

        [Fact]
        public async Task WhenProcessMessageBatchThrows_CancellationTokenCancelled()
        {
            var ex = new Exception("TEST");

            var cancellationTokenSource = new CancellationTokenSource();
            await _defaultBehaviour.Invoke(() => throw ex, cancellationTokenSource);

            Assert.True(cancellationTokenSource.IsCancellationRequested);

        }
    }
}
