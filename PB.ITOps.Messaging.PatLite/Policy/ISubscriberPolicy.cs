using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.Policy
{
    public interface ISubscriberPolicy
    {
        ISubscriberPolicy PreviousPolicy { get; set; }
        Task ProcessMessage(Func<BrokeredMessage, Task> action, BrokeredMessage message);
        void ProcessMessageBatch(Action action, CancellationTokenSource tokenSource);
        void OnComplete(BrokeredMessage message);
        void OnFailure(BrokeredMessage message, Exception ex);
        ISubscriberPolicy ChainPolicy(ISubscriberPolicy previousPolicy);

    }
}