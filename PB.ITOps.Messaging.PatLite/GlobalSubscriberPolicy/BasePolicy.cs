using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy
{
    public abstract class BasePolicy: ISubscriberPolicy
    {
        public ISubscriberPolicy InnerPolicy { get; set; }

        public async Task ProcessMessage(Func<BrokeredMessage, Task> action, BrokeredMessage message)
        {
            if (InnerPolicy != null)
            {
                await DoProcessMessage(msg => InnerPolicy.ProcessMessage(action, message), message);
                await InnerPolicy.ProcessMessage((msg) => DoProcessMessage(action, msg), message);
            }
            else
            {
                await DoProcessMessage(action, message);
            }
        }

        public async Task<int> ProcessMessageBatch(Func<Task<int>> action, CancellationTokenSource tokenSource)
        {
            if (InnerPolicy != null)
            {
                return await DoProcessMessageBatch(() => InnerPolicy.ProcessMessageBatch(action, tokenSource), tokenSource);
            }
            else
            {
                return await DoProcessMessageBatch(action, tokenSource);
            }
        }

        public async Task OnMessageHandlerCompleted(BrokeredMessage message, string body)
        {
            if (await MessageHandlerCompleted(message, body))
            {
                var onComplete = InnerPolicy?.OnMessageHandlerCompleted(message, body);
                if (onComplete != null) await onComplete;
            }
        }
        protected virtual Task<bool> MessageHandlerCompleted(BrokeredMessage message, string body)
        {
            return Task.FromResult(true);
        }

        public async Task OnMessageHandlerFailed(BrokeredMessage message, string body, Exception ex)
        {
            if (await MessageHandlerFailed(message, body, ex))
            {
                var onFailure = InnerPolicy?.OnMessageHandlerFailed(message, body, ex);
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
        protected virtual Task<bool> MessageHandlerFailed(BrokeredMessage message, string body, Exception ex)
        {
            return Task.FromResult(true);
        }

        public ISubscriberPolicy AppendInnerPolicy(ISubscriberPolicy innerPolicy)
        {
            InnerPolicy = innerPolicy;
            return this;
        }

        protected virtual Task<int> DoProcessMessageBatch(Func<Task<int>> action, CancellationTokenSource tokenSource)
        {
            return action();
        }

        protected virtual Task DoProcessMessage(Func<BrokeredMessage, Task> action, BrokeredMessage message)
        {
            return action(message);
        }
    }
}