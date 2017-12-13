using System;
using System.Threading.Tasks;
using PB.ITOps.Messaging.PatLite.MessageProcessing;

namespace PB.ITOps.Messaging.PatLite.CicuitBreaker
{
    public class CircuitBreakerMessageProcessingBehaviour: IMessageProcessingBehaviour
    {
        private readonly CircuitBreakerBatchProcessingBehaviour _circuitBreakerBatchProcessingBehaviour;

        public CircuitBreakerMessageProcessingBehaviour(CircuitBreakerBatchProcessingBehaviour circuitBreakerBatchProcessingBehaviour)
        {
            _circuitBreakerBatchProcessingBehaviour = circuitBreakerBatchProcessingBehaviour;
        }
        public async Task Invoke(Func<MessageContext, Task> next, MessageContext messageContext)
        {
            try
            {
                await next(messageContext).ConfigureAwait(false);
                _circuitBreakerBatchProcessingBehaviour.MessageCompleted();
            }
            catch (Exception ex)
            {
                _circuitBreakerBatchProcessingBehaviour.MessageFailed(ex);
                throw;
            }
        }
    }
}
