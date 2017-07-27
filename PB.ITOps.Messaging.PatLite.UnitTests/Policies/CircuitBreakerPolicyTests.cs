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
    public class CircuitBreakerPolicyTests
    {
        private readonly TestCircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly ICircuitBreakerEvents _events;

        public class TestCircuitBreakerPolicy : CircuitBreakerPolicy {
            public TestCircuitBreakerPolicy(ILog log, SubscriberConfiguration config, CircuitBreakerOptions circuitBreakerOptions) : base(log, config, circuitBreakerOptions)
            {
            }

            protected override bool ShouldCircuitBreak(Exception exception)
            {
                return SubstituteShouldCircuitBreak(exception);
            }

            public virtual bool SubstituteShouldCircuitBreak(Exception exception)
            {
                throw new NotImplementedException();
            }
    }


        public CircuitBreakerPolicyTests()
        {
            var args = new object[]
            {
                Substitute.For<ILog>(),
                new SubscriberConfiguration
                {
                    SubscriberName = "TestSubscriber"
                },
                new CircuitBreakerPolicy.CircuitBreakerOptions(1)
            };
            _events = Substitute.For<ICircuitBreakerEvents>();
            _circuitBreakerPolicy = Substitute.ForPartsOf<TestCircuitBreakerPolicy>(args);
            _circuitBreakerPolicy.CircuitBroken += _events.Broken;
            _circuitBreakerPolicy.CircuitReset += _events.Reset;
            _circuitBreakerPolicy.CircuitTest += _events.TestCircuit;
        }

        public interface ICircuitBreakerEvents
        {
            void Broken(object sender, EventArgs args);
            void Reset(object sender, EventArgs args);
            void TestCircuit(object sender, EventArgs args);
        }

        [Fact]
        public async Task WhenMessageSucceeds_ThenCircuitRemainsClosed()
        {
            var action = new Func<Task<int>>(async () => 
            {
                await _circuitBreakerPolicy.OnMessageHandlerCompleted(new BrokeredMessage(), null);
                return 1;
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await _circuitBreakerPolicy.ProcessMessageBatch(action, cancellationTokenSource);
            }

            _events.DidNotReceiveWithAnyArgs().Broken(null, null);
        }

        [Fact]
        public async Task WhenMessageFailsWithNonCircuitBreakingException_ThenBreakCircuitIsNotCalled()
        {
            _circuitBreakerPolicy.SubstituteShouldCircuitBreak(Arg.Any<Exception>()).ReturnsForAnyArgs(false);
            var action = new Func<Task<int>>(async () =>
            {
                await _circuitBreakerPolicy.OnMessageHandlerFailed(new BrokeredMessage(), null, new Exception("Non circuit breaking"));
                return 1;
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await _circuitBreakerPolicy.ProcessMessageBatch(action, cancellationTokenSource);
            }

            _events.DidNotReceiveWithAnyArgs().Broken(null, null);
        }

        [Fact]
        public async Task WhenMessageFailsWithCircuitBreakingException_ThenBreakCircuitIsCalled()
        {
            _circuitBreakerPolicy.SubstituteShouldCircuitBreak(Arg.Any<Exception>()).ReturnsForAnyArgs(true);
            var action = new Func<Task<int>>(async () =>
            {
                await _circuitBreakerPolicy.OnMessageHandlerFailed(new BrokeredMessage(), null, new Exception());
                return 1;
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await _circuitBreakerPolicy.ProcessMessageBatch(action, cancellationTokenSource);
            }

            _events.ReceivedWithAnyArgs(1).Broken(null, null);
        }

        [Fact]
        public async Task WhenCircuitBroken_OnNextMessage_ThenCircuitIsTested()
        {
            _circuitBreakerPolicy.SubstituteShouldCircuitBreak(Arg.Any<Exception>()).ReturnsForAnyArgs(true);
            var actionFailure = new Func<Task<int>>(async () =>
            {
                await _circuitBreakerPolicy.OnMessageHandlerFailed(new BrokeredMessage(), null, new Exception());
                return 1;
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await _circuitBreakerPolicy.ProcessMessageBatch(actionFailure, cancellationTokenSource);
                await _circuitBreakerPolicy.ProcessMessageBatch(actionFailure, cancellationTokenSource);
            }

            _events.ReceivedWithAnyArgs(1).TestCircuit(null, null);
        }

        [Fact]
        public async Task WhenCircuitBroken_AndNextMessageFailesWithNoCircuitBreakingError_ThenCircuitIsReset()
        {
            var nonCircuitBreakingException = new Exception("Non circuit breaking");
            _circuitBreakerPolicy.SubstituteShouldCircuitBreak(Arg.Is<Exception>(e => e == nonCircuitBreakingException)).Returns(false);
            var actionFailure = new Func<Task<int>>(async () =>
            {
                await _circuitBreakerPolicy.OnMessageHandlerFailed(new BrokeredMessage(), null, nonCircuitBreakingException);
                return 1;
            });
            var circuitBreakingException = new Exception("Circuit breaking");
            _circuitBreakerPolicy.SubstituteShouldCircuitBreak(Arg.Is<Exception>(e => e == circuitBreakingException)).Returns(true);
            var actionFailureAndBreakCircuit = new Func<Task<int>>(async () =>
            {
                await _circuitBreakerPolicy.OnMessageHandlerFailed(new BrokeredMessage(), null, circuitBreakingException);
                return 1;
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await _circuitBreakerPolicy.ProcessMessageBatch(actionFailureAndBreakCircuit, cancellationTokenSource);
                await _circuitBreakerPolicy.ProcessMessageBatch(actionFailure, cancellationTokenSource);
            }

            _events.ReceivedWithAnyArgs(1).Reset(null, null);
        }

        [Fact]
        public async Task WhenCircuitBroken_AndNextMessageCompletesSuccessfully_ThenCircuitIsReset()
        {
            _circuitBreakerPolicy.SubstituteShouldCircuitBreak(Arg.Any<Exception>()).ReturnsForAnyArgs(true);
            var actionFailure = new Func<Task<int>>(async () =>
            {
                await _circuitBreakerPolicy.OnMessageHandlerFailed(new BrokeredMessage(), null, new Exception());
                return 1;
            });
            var actionSuccess = new Func<Task<int>>(async () =>
            {
                await _circuitBreakerPolicy.OnMessageHandlerCompleted(new BrokeredMessage(), null);
                return 1;
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await _circuitBreakerPolicy.ProcessMessageBatch(actionFailure, cancellationTokenSource);
                await _circuitBreakerPolicy.ProcessMessageBatch(actionSuccess, cancellationTokenSource);
            }

            _events.ReceivedWithAnyArgs(1).Reset(null, null);
        }
    }

}
