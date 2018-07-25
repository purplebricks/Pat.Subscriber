using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Pat.Subscriber.BatchProcessing;
using Pat.Subscriber.CicuitBreaker;
using Pat.Subscriber.MessageProcessing;
using Xunit;

namespace Pat.Subscriber.UnitTests.Behaviours
{
    public class MockHandleMessageBehaviour : IMessageProcessingBehaviour
    {
        public Action Action { get; set; }
        public Task Invoke(Func<MessageContext, Task> next, MessageContext messageContext)
        {
            Action();
            return Task.CompletedTask;
        }
    }

    public class MockDefaultMessageBehaviour : IMessageProcessingBehaviour
    {
        public  async Task Invoke(Func<MessageContext, Task> next, MessageContext messageContext)
        {
            try
            {
                await next(messageContext);
            }
            catch
            {
                // ignored
            }
        }
    }


    public class CircuitBreakerTests
    {
        private readonly CircuitBreakerBatchProcessingBehaviour _circuitBreakerBatchProcessingBehaviour;
        private readonly BatchProcessingBehaviourPipeline _batchProcessingBehaviourPipeline;
        private readonly ICircuitBreakerEvents _events;
        private readonly MessageProcessingBehaviourPipeline _messageProcessingPipeline;
        private readonly MockHandleMessageBehaviour _mockHandleMessageBehaviour;
        private MessageContext _messageContext;

        public CircuitBreakerTests()
        {
            _messageContext = new MessageContext
            {
                Message = new Message(),
                MessageReceiver = Substitute.For<IMessageReceiver>()
            };

            var config = new SubscriberConfiguration
            {
                SubscriberName = "TestSubscriber"
            };
            _events = Substitute.For<ICircuitBreakerEvents>();

            var circuitBreakerOptions = new CircuitBreakerBatchProcessingBehaviour.CircuitBreakerOptions(1, exception => false);
            circuitBreakerOptions.CircuitBroken += _events.Broken;
            circuitBreakerOptions.CircuitReset += _events.Reset;
            circuitBreakerOptions.CircuitTest += _events.TestCircuit;

            _circuitBreakerBatchProcessingBehaviour = new CircuitBreakerBatchProcessingBehaviour(Substitute.For<ILogger>(),
                config, circuitBreakerOptions);

            _batchProcessingBehaviourPipeline = new BatchProcessingBehaviourPipeline()
                .AddBehaviour(_circuitBreakerBatchProcessingBehaviour)
                .AddBehaviour(new DefaultBatchProcessingBehaviour(Substitute.For<ILogger>(), config));

            _mockHandleMessageBehaviour = new MockHandleMessageBehaviour();
            var circuitBreakerMessageProcessingBehaviour = new CircuitBreakerMessageProcessingBehaviour(_circuitBreakerBatchProcessingBehaviour);
            
            _messageProcessingPipeline = new MessageProcessingBehaviourPipeline()
                .AddBehaviour(new DefaultMessageProcessingBehaviour(Substitute.For<ILogger>(), config))
                .AddBehaviour(circuitBreakerMessageProcessingBehaviour)
                .AddBehaviour(_mockHandleMessageBehaviour);

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
            _mockHandleMessageBehaviour.Action = () => { };

            var action = new Func<Task<int>>(async () =>
            {
                await _messageProcessingPipeline.Invoke(new MessageContext());
                return 1;
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await _batchProcessingBehaviourPipeline.Invoke(action, cancellationTokenSource);
            }

            _events.DidNotReceiveWithAnyArgs().Broken(null, null);
        }

        [Fact]
        public async Task WhenMessageFailsWithNonCircuitBreakingException_ThenBreakCircuitIsNotCalled()
        {
            _mockHandleMessageBehaviour.Action = () => throw new Exception("Non circuit breaking");
            _circuitBreakerBatchProcessingBehaviour.ShouldCircuitBreak = exception => false;

            var action = new Func<Task<int>>(async () =>
            {
                await _messageProcessingPipeline.Invoke(_messageContext);
                return 1;
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await _batchProcessingBehaviourPipeline.Invoke(action, cancellationTokenSource);
            }

            _events.DidNotReceiveWithAnyArgs().Broken(null, null);
        }

        [Fact]
        public async Task WhenMessageFailsWithCircuitBreakingException_ThenBreakCircuitIsCalled()
        {
            _mockHandleMessageBehaviour.Action = () => throw new Exception();
            _circuitBreakerBatchProcessingBehaviour.ShouldCircuitBreak = exception => true;

            var action = new Func<Task<int>>(async () =>
            {
                await _messageProcessingPipeline.Invoke(_messageContext);
                return 1;
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await _batchProcessingBehaviourPipeline.Invoke(action, cancellationTokenSource);
            }

            _events.ReceivedWithAnyArgs(1).Broken(null, null);
        }

        [Fact]
        public async Task WhenCircuitBroken_OnNextMessage_ThenCircuitIsTested()
        {
            _mockHandleMessageBehaviour.Action = () => throw new Exception();
            _circuitBreakerBatchProcessingBehaviour.ShouldCircuitBreak = exception => true;
            var actionFailure = new Func<Task<int>>(async () =>
            {
                await _messageProcessingPipeline.Invoke(_messageContext);
                return 1;
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await _batchProcessingBehaviourPipeline.Invoke(actionFailure, cancellationTokenSource);
                await _batchProcessingBehaviourPipeline.Invoke(actionFailure, cancellationTokenSource);
            }

            _events.ReceivedWithAnyArgs(1).TestCircuit(null, null);
        }

        [Fact]
        public async Task WhenCircuitBroken_AndNextMessageFailesWithNoCircuitBreakingError_ThenCircuitIsReset()
        {
            var nonCircuitBreakingException = new Exception("Non circuit breaking");
            var circuitBreakingException = new Exception("Circuit breaking");
            _circuitBreakerBatchProcessingBehaviour.ShouldCircuitBreak = exception => exception == circuitBreakingException;
            var actionFailure = new Func<Task<int>>(async () =>
            {
                _mockHandleMessageBehaviour.Action = () => throw nonCircuitBreakingException;
                await _messageProcessingPipeline.Invoke(_messageContext);
                return 1;
            });
            var actionFailureAndBreakCircuit = new Func<Task<int>>(async () =>
            {
                _mockHandleMessageBehaviour.Action = () => throw circuitBreakingException;
                await _messageProcessingPipeline.Invoke(_messageContext);
                return 1;
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await _batchProcessingBehaviourPipeline.Invoke(actionFailureAndBreakCircuit, cancellationTokenSource);
                await _batchProcessingBehaviourPipeline.Invoke(actionFailure, cancellationTokenSource);
            }

            _events.ReceivedWithAnyArgs(1).Reset(null, null);
        }

        [Fact]
        public async Task WhenCircuitBroken_AndNextMessageCompletesSuccessfully_ThenCircuitIsReset()
        {
            _circuitBreakerBatchProcessingBehaviour.ShouldCircuitBreak = exception => true;
            var actionFailure = new Func<Task<int>>(async () =>
            {
                _mockHandleMessageBehaviour.Action = () => throw new Exception();
                await _messageProcessingPipeline.Invoke(_messageContext);
                return 1;
            });
            var actionSuccess = new Func<Task<int>>(async () =>
            {
                _mockHandleMessageBehaviour.Action = () => { };
                await _messageProcessingPipeline.Invoke(_messageContext);
                return 1;
            });

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await _batchProcessingBehaviourPipeline.Invoke(actionFailure, cancellationTokenSource);
                await _batchProcessingBehaviourPipeline.Invoke(actionSuccess, cancellationTokenSource);
            }

            _events.ReceivedWithAnyArgs(1).Reset(null, null);
        }
    }

}
