using System;
using System.Threading.Tasks;
using log4net;
using Pat.Subscriber.BatchProcessing;

namespace Pat.Subscriber.CicuitBreaker
{
    public class CircuitBreakerBatchProcessingBehaviour : IBatchProcessingBehaviour
    {
        public class CircuitBreakerOptions
        {
            public CircuitBreakerOptions(int circuitTestIntervalInSeconds, Func<Exception, bool> shouldCircuitBreak)
            {
                ShouldCircuitBreak = shouldCircuitBreak;
                CircuitTestIntervalInSeconds = circuitTestIntervalInSeconds;
            }

            public int CircuitTestIntervalInSeconds { get; }
            public Func<Exception, bool> ShouldCircuitBreak { get; }
            public CircuitBrokenHandler CircuitBroken;
            public CircuitResetHandler CircuitReset;
            public CircuitTestHandler CircuitTest;
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
        private readonly int _circuitTestIntervalInSeconds;
        public Func<Exception, bool> ShouldCircuitBreak { get; set; }
        private event CircuitBrokenHandler CircuitBroken;
        private event CircuitResetHandler CircuitReset;
        private event CircuitTestHandler CircuitTest;

        public CircuitBreakerBatchProcessingBehaviour(ILog log, SubscriberConfiguration config, CircuitBreakerOptions circuitBreakerOptions)
        {
            _log = log;
            _config = config;
            ShouldCircuitBreak = circuitBreakerOptions.ShouldCircuitBreak;
            _circuitTestIntervalInSeconds = circuitBreakerOptions.CircuitTestIntervalInSeconds;
            State = CircuitState.Closed;
            CircuitBroken += circuitBreakerOptions.CircuitBroken;
            CircuitReset += circuitBreakerOptions.CircuitReset;
            CircuitTest += circuitBreakerOptions.CircuitTest;
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
        

        public void MessageCompleted()
        {
            if (State == CircuitState.HalfOpen)
            {
                CloseCircuit();
            }
        }

        public void MessageFailed(Exception ex)
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

        public Task TestCircuit(Func<BatchContext, Task> next, BatchContext context)
        {
            OnCircuitTest(EventArgs.Empty);
            _log.Info($"Subscriber circuit breaker: testing circuit for subscriber '{_config.SubscriberName}'");
            State = CircuitState.HalfOpen;
            return next(context);
        }

        public Task Invoke(Func<BatchContext, Task> next, BatchContext context)
        {
            if (State == CircuitState.Closed)
            {
                return next(context);
            }
            else
            {
                Task.Delay(TimeSpan.FromSeconds(_circuitTestIntervalInSeconds), context.TokenSource.Token).Wait();
                return TestCircuit(next, context);
            }
        }
    }
}
