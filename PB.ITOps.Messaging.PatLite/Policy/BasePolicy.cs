using System;
using System.Threading;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.Policy
{
    public abstract class BasePolicy: ISubscriberPolicy
    {
        public ISubscriberPolicy PreviousPolicy { get; set; }

        public void Execute(Action action, CancellationTokenSource tokenSource)
        {
            if (PreviousPolicy != null)
            {
                PreviousPolicy.Execute(() => DoAction(action, tokenSource), tokenSource);
            }
            else
            {
                DoAction(action, tokenSource);
            }
        }

        public abstract void OnComplete(BrokeredMessage message);

        public abstract void OnFailure(BrokeredMessage message, Exception ex);

        protected virtual void DoAction(Action action, CancellationTokenSource tokenSource)
        {
            action();
        }
    }
}