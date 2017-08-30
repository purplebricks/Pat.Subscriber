using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus.Messaging;
using NSubstitute;
using PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy;
using Xunit;

namespace PB.ITOps.Messaging.PatLite.UnitTests.Policies
{
    public class StandardPolicyTests
    {
        private readonly ILog _log;
        private readonly StandardPolicy _standardPolicy;

        public StandardPolicyTests()
        {
            _log = Substitute.For<ILog>();
            _standardPolicy = new StandardPolicy(_log, new SubscriberConfiguration());
        }

        [Fact]
        public async Task WhenProcessMessageThrows_ExceptionAllowedToPropogate()
        {
            var ex = new Exception("TEST");

            await Assert.ThrowsAsync<Exception>(() => _standardPolicy.ProcessMessage(message => throw ex, new BrokeredMessage()));
        }

        [Fact]
        public async Task WhenProcessMessageThrows_InfrastructureErrorLogged()
        {
            var ex = new Exception("TEST");

            try
            {
                await _standardPolicy.ProcessMessage(message => throw ex, new BrokeredMessage());
            }
            catch (Exception)
            {
                
            }

            _log.Received(1).Fatal(Arg.Is<string>(m => m.Contains("Unhandled infrastructure exception")), ex);
        }

        [Fact]
        public async Task WhenProcessMessageBatchThrows_InfrastructureErrorLogged()
        {
            var ex = new Exception("TEST");
  
            await _standardPolicy.ProcessMessageBatch(() => throw ex, new CancellationTokenSource());

            _log.Received(1).Fatal(Arg.Is<string>(m => m.Contains("Unhandled non transient exception on queue")), ex);
        }

        [Fact]
        public async Task WhenProcessMessageBatchThrows_CancellationTokenCancelled()
        {
            var ex = new Exception("TEST");

            var cancellationTokenSource = new CancellationTokenSource();
            await _standardPolicy.ProcessMessageBatch(() => throw ex, cancellationTokenSource);

            Assert.True(cancellationTokenSource.IsCancellationRequested);

        }
    }
}
