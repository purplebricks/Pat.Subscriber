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
        private readonly ILogger<DefaultBatchProcessingBehaviour> _log;
        private readonly BatchProcessingBehaviourPipeline _defaultBehaviour;

        public DefaultBatchBehaviourTests()
        {
            _log = Substitute.For<ILogger<DefaultBatchProcessingBehaviour>>();
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
                0,
                Arg.Is<object>(m => m.ToString().Contains("Unhandled non transient exception on queue")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>());
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
