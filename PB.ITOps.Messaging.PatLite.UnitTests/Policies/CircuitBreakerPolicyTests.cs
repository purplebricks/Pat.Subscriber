using System;
using System.Threading;
using log4net;
using Microsoft.ServiceBus.Messaging;
using NSubstitute;
using PB.ITOps.Messaging.PatLite.Policy;
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
        public void WhenMessageSucceeds_ThenCircuitRemainsClosed()
        {
            var action = new Action(() => { _circuitBreakerPolicy.OnComplete(new BrokeredMessage()); });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                _circuitBreakerPolicy.ProcessMessageBatch(action, cancellationTokenSource);
            }

            _events.DidNotReceiveWithAnyArgs().Broken(null, null);
        }

        [Fact]
        public void WhenMessageFailsWithNonCircuitBreakingException_ThenBreakCircuitIsNotCalled()
        {
            _circuitBreakerPolicy.SubstituteShouldCircuitBreak(Arg.Any<Exception>()).ReturnsForAnyArgs(false);
            var action = new Action(() => { _circuitBreakerPolicy.OnFailure(new BrokeredMessage(), new Exception("Non circuit breaking")); });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                _circuitBreakerPolicy.ProcessMessageBatch(action, cancellationTokenSource);
            }

            _events.DidNotReceiveWithAnyArgs().Broken(null, null);
        }

        [Fact]
        public void WhenMessageFailsWithCircuitBreakingException_ThenBreakCircuitIsCalled()
        {
            _circuitBreakerPolicy.SubstituteShouldCircuitBreak(Arg.Any<Exception>()).ReturnsForAnyArgs(true);
            var action = new Action(() =>
            {
                _circuitBreakerPolicy.OnFailure(new BrokeredMessage(), new Exception());
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                _circuitBreakerPolicy.ProcessMessageBatch(action, cancellationTokenSource);
            }

            _events.ReceivedWithAnyArgs(1).Broken(null, null);
        }

        [Fact]
        public void WhenCircuitBroken_OnNextMessage_ThenCircuitIsTested()
        {
            _circuitBreakerPolicy.SubstituteShouldCircuitBreak(Arg.Any<Exception>()).ReturnsForAnyArgs(true);
            var actionFailure = new Action(() =>
            {
                _circuitBreakerPolicy.OnFailure(new BrokeredMessage(), new Exception());
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                _circuitBreakerPolicy.ProcessMessageBatch(actionFailure, cancellationTokenSource);
                _circuitBreakerPolicy.ProcessMessageBatch(actionFailure, cancellationTokenSource);
            }

            _events.ReceivedWithAnyArgs(1).TestCircuit(null, null);
        }

        [Fact]
        public void WhenCircuitBroken_AndNextMessageFailesWithNoCircuitBreakingError_ThenCircuitIsReset()
        {
            var nonCircuitBreakingException = new Exception("Non circuit breaking");
            _circuitBreakerPolicy.SubstituteShouldCircuitBreak(Arg.Is<Exception>(e => e == nonCircuitBreakingException)).Returns(false);
            var actionFailure = new Action(() =>
            {
                _circuitBreakerPolicy.OnFailure(new BrokeredMessage(), nonCircuitBreakingException);
            });
            var circuitBreakingException = new Exception("Circuit breaking");
            _circuitBreakerPolicy.SubstituteShouldCircuitBreak(Arg.Is<Exception>(e => e == circuitBreakingException)).Returns(true);
            var actionFailureAndBreakCircuit = new Action(() =>
            {
                _circuitBreakerPolicy.OnFailure(new BrokeredMessage(), circuitBreakingException);
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                _circuitBreakerPolicy.ProcessMessageBatch(actionFailureAndBreakCircuit, cancellationTokenSource);
                _circuitBreakerPolicy.ProcessMessageBatch(actionFailure, cancellationTokenSource);
            }

            _events.ReceivedWithAnyArgs(1).Reset(null, null);
        }

        [Fact]
        public void WhenCircuitBroken_AndNextMessageCompletesSuccessfully_ThenCircuitIsReset()
        {
            _circuitBreakerPolicy.SubstituteShouldCircuitBreak(Arg.Any<Exception>()).ReturnsForAnyArgs(true);
            var actionFailure = new Action(() =>
            {
                _circuitBreakerPolicy.OnFailure(new BrokeredMessage(), new Exception());
            });
            var actionSuccess = new Action(() =>
            {
                _circuitBreakerPolicy.OnComplete(new BrokeredMessage());
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                _circuitBreakerPolicy.ProcessMessageBatch(actionFailure, cancellationTokenSource);
                _circuitBreakerPolicy.ProcessMessageBatch(actionSuccess, cancellationTokenSource);
            }

            _events.ReceivedWithAnyArgs(1).Reset(null, null);
        }
    }

}
