using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.Policy
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

        protected override void DoProcessMessageBatch(Action action, CancellationTokenSource tokenSource)
        {
            if (State == CircuitState.Closed)
            {
                action();
            }
            else
            {
                Task.Delay(_circuitTestInterval, tokenSource.Token).Wait();
                TestCircuit(action);
            }
        }

        public override void OnComplete(BrokeredMessage message)
        {
            base.OnComplete(message);
            if (State == CircuitState.HalfOpen)
            {
                CloseCircuit();
            }
        }

        public override void OnFailure(BrokeredMessage message, Exception ex)
        {
            if (ShouldCircuitBreak(ex))
            {
                BreakCircuit();
            }
            else if (State == CircuitState.HalfOpen)
            {
                CloseCircuit();
            }
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

        public void TestCircuit(Action action)
        {
            OnCircuitTest(EventArgs.Empty);
            _log.Info($"Subscriber circuit breaker: testing circuit for subscriber '{_config.SubscriberName}'");
            State = CircuitState.HalfOpen;
            action();
        }
    }
}
