using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy
{
    public class CircuitBreakerPolicy : BasePolicy
    {
        public class CircuitBreakerOptions
        {
            public CircuitBreakerOptions(int circuitTestInterval, Func<Exception, bool> shouldCircuitBreak)
            {
                ShouldCircuitBreak = shouldCircuitBreak;
                CircuitTestInterval = circuitTestInterval;
            }

            public int CircuitTestInterval { get; }
            public Func<Exception, bool> ShouldCircuitBreak { get; }
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
        public Func<Exception, bool> ShouldCircuitBreak { get; set; }
        public event CircuitBrokenHandler CircuitBroken;
        public event CircuitResetHandler CircuitReset;
        public event CircuitResetHandler CircuitTest;

        public CircuitBreakerPolicy(ILog log, SubscriberConfiguration config, CircuitBreakerOptions circuitBreakerOptions)
        {
            _log = log;
            _config = config;
            ShouldCircuitBreak = circuitBreakerOptions.ShouldCircuitBreak;
            _circuitTestInterval = circuitBreakerOptions.CircuitTestInterval;
            State = CircuitState.Closed;
        }

        private void OnCircuitBroken(EventArgs e)
        {
            CircuitBroken?.Invoke(this, e);
        }

        private void OnCircuitReset(EventArgs e)
        {
            CircuitReset?.Invoke(this, e);
        }

        private void OnCircuitTest(EventArgs e)
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
