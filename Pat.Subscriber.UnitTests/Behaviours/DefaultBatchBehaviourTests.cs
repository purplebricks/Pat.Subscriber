using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using NSubstitute;
using Pat.Subscriber.BatchProcessing;
using Xunit;

namespace Pat.Subscriber.UnitTests.Behaviours
{
    public class DefaultBatchBehaviourTests
    {
        private readonly ILog _log;
        private readonly BatchProcessingBehaviourPipeline _defaultBehaviour;

        public DefaultBatchBehaviourTests()
        {
            _log = Substitute.For<ILog>();
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
            
            _log.Received(1).Fatal(Arg.Is<string>(m => m.Contains("Unhandled non transient exception on queue")), ex);
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
