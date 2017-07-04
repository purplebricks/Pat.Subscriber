using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.Policy
{
    public abstract class BasePolicy: ISubscriberPolicy
    {
        public ISubscriberPolicy PreviousPolicy { get; set; }

        public async Task ProcessMessage(Func<BrokeredMessage, Task> action, BrokeredMessage message)
        {
            if (PreviousPolicy != null)
            {
                await PreviousPolicy.ProcessMessage((msg) => DoProcessMessage(action, msg), message);
            }
            else
            {
                await DoProcessMessage(action, message);
            }
        }

        public void ProcessMessageBatch(Action action, CancellationTokenSource tokenSource)
        {
            if (PreviousPolicy != null)
            {
                PreviousPolicy.ProcessMessageBatch(() => DoProcessMessageBatch(action, tokenSource), tokenSource);
            }
            else
            {
                DoProcessMessageBatch(action, tokenSource);
            }
        }

        public virtual void OnComplete(BrokeredMessage message)
        {
            PreviousPolicy?.OnComplete(message);
        }

        public virtual void OnFailure(BrokeredMessage message, Exception ex)
        {
            PreviousPolicy?.OnFailure(message, ex);
        }
        public ISubscriberPolicy ChainPolicy(ISubscriberPolicy previousPolicy)
        {
            PreviousPolicy = previousPolicy;
            return this;
        }

        protected virtual void DoProcessMessageBatch(Action action, CancellationTokenSource tokenSource)
        {
            action();
        }

        protected virtual async Task DoProcessMessage(Func<BrokeredMessage, Task> action, BrokeredMessage message)
        {
            await action(message);
        }
    }
}