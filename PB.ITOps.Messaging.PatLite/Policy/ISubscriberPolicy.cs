using System;
using System.Threading;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.Policy
{
    public interface ISubscriberPolicy
    {
        ISubscriberPolicy PreviousPolicy { get; set; }
        void Execute(Action action, CancellationTokenSource tokenSource);
        void OnComplete(BrokeredMessage message);
        void OnFailure(BrokeredMessage message, Exception ex);
    }
}