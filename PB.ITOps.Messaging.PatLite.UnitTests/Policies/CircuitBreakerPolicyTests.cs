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
        private readonly CircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly ICircuitBreakerEvents _events;

        public CircuitBreakerPolicyTests()
        {
            var config = new SubscriberConfiguration
            {
                SubscriberName = "TestSubscriber"
            };
            _events = Substitute.For<ICircuitBreakerEvents>();
            _circuitBreakerPolicy = new CircuitBreakerPolicy(Substitute.For<ILog>(), config, new CircuitBreakerPolicy.CircuitBreakerOptions(1, exception => false));
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
            _circuitBreakerPolicy.ShouldCircuitBreak = exception => false;

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
            _circuitBreakerPolicy.ShouldCircuitBreak = exception => true;

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
            _circuitBreakerPolicy.ShouldCircuitBreak = exception => true;
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
            var circuitBreakingException = new Exception("Circuit breaking");
            _circuitBreakerPolicy.ShouldCircuitBreak = exception => exception == circuitBreakingException;
            var actionFailure = new Func<Task<int>>(async () =>
            {
                await _circuitBreakerPolicy.OnMessageHandlerFailed(new BrokeredMessage(), null, nonCircuitBreakingException);
                return 1;
            });
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
            _circuitBreakerPolicy.ShouldCircuitBreak = exception => true;
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
