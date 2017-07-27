using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.MessageProcessingPolicy
{
    public abstract class BaseMessageProcessingPolicy : IMessageProcessingPolicy
    {
        public IMessageProcessingPolicy NextPolicy { get; private set; }
        public async Task OnMessageHandlerCompleted(BrokeredMessage message, string body)
        {
            if (await MessageHandlerCompleted(message, body))
            {
                var onComplete = NextPolicy?.OnMessageHandlerCompleted(message, body);
                if (onComplete != null) await onComplete;
            }
        }

        /// <summary>
        /// Action to take on successful message processing.
        /// Return true to allow next policy in line to be called
        /// Return false to short-circuit further processing
        /// </summary>
        /// <param name="message"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        protected abstract Task<bool> MessageHandlerCompleted(BrokeredMessage message, string body);
       
        public async Task OnMessageHandlerFailed(BrokeredMessage message, string body, Exception ex)
        {
            if (await MessageHandlerFailed(message, body, ex))
            {
                var onFailure = NextPolicy?.OnMessageHandlerFailed(message, body, ex);
                if (onFailure != null) await onFailure;
            }
        }

        /// <summary>
        /// Action to take on message processing failure.
        /// Return true to allow next policy in line to be called
        /// Return false to short-circuit further processing
        /// </summary>
        /// <param name="message"></param>
        /// <param name="body"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected abstract Task<bool> MessageHandlerFailed(BrokeredMessage message, string body, Exception ex);

        public IMessageProcessingPolicy AppendPolicy(IMessageProcessingPolicy nextPolicy)
        {
            NextPolicy = nextPolicy;
            return NextPolicy;
        }
    }
}