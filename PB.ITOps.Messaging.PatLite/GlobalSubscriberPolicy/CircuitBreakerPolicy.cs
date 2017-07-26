using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy
{
    public abstract class CircuitBreakerPolicy : BasePolicy
    {
        public class CircuitBreakerOptions
        {
            public CircuitBreakerOptions(int circuitTestInterval)
            {
                CircuitTestInterval = circuitTestInterval;
            }

            public int CircuitTestInterval { get; private set; }
        }

        public delegate void CircuitBrokenHandler(object sender, EventArgs e);
        public delegate void CircuitTestHandler(object sender, EventArgs e);
        public delegate void CircuitResetHandler(object sender, EventArgs e);

        public enum CircuitState
        {
            Closed,
            HalfOpen,
            Open
        }
        public CircuitState State { get; private set; }
        private readonly ILog _log;
        private readonly SubscriberConfiguration _config;
        private readonly int _circuitTestInterval;
        public event CircuitBrokenHandler CircuitBroken;
        public event CircuitResetHandler CircuitReset;
        public event CircuitResetHandler CircuitTest;

        protected CircuitBreakerPolicy(ILog log, SubscriberConfiguration config, CircuitBreakerOptions circuitBreakerOptions)
        {
            _log = log;
            _config = config;
            _circuitTestInterval = circuitBreakerOptions.CircuitTestInterval;
            State = CircuitState.Closed;
        }

        protected virtual void OnCircuitBroken(EventArgs e)
        {
            CircuitBroken?.Invoke(this, e);
        }

        protected virtual void OnCircuitReset(EventArgs e)
        {
            CircuitReset?.Invoke(this, e);
        }

        protected virtual void OnCircuitTest(EventArgs e)
        {
            CircuitTest?.Invoke(this, e);
        }

        protected override Task<int> DoProcessMessageBatch(Func<Task<int>> action, CancellationTokenSource tokenSource)
        {
            if (State == CircuitState.Closed)
            {
                return action();
            }
            else
            {
                Task.Delay(_circuitTestInterval, tokenSource.Token).Wait();
                return TestCircuit(action);
            }
        }

        protected override Task<bool> MessageHandlerCompleted(BrokeredMessage message, string body)
        {
            if (State == CircuitState.HalfOpen)
            {
                CloseCircuit();
            }

            return Task.FromResult(true);
        }

        protected override Task<bool> MessageHandlerFailed(BrokeredMessage message, string body, Exception ex)
        {
            if (ShouldCircuitBreak(ex))
            {
                BreakCircuit();
            }
            else if (State == CircuitState.HalfOpen)
            {
                CloseCircuit();
            }

            return Task.FromResult(true);
        }

        protected abstract bool ShouldCircuitBreak(Exception exception);

        public void BreakCircuit()
        {
            _log.Warn($"Subscriber circuit breaker: circuit opened for subscriber '{_config.SubscriberName}'");
            State = CircuitState.Open;
            OnCircuitBroken(EventArgs.Empty);
        }

        public void CloseCircuit()
        {
            _log.Info($"Subscriber circuit breaker: circuit closed for subscriber '{_config.SubscriberName}'");
            State = CircuitState.Closed;
            OnCircuitReset(EventArgs.Empty);
        }

        public Task<int> TestCircuit(Func<Task<int>> action)
        {
            OnCircuitTest(EventArgs.Empty);
            _log.Info($"Subscriber circuit breaker: testing circuit for subscriber '{_config.SubscriberName}'");
            State = CircuitState.HalfOpen;
            return action();
        }
    }
}
