using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy
{
    public interface ISubscriberPolicy
    {
        ISubscriberPolicy InnerPolicy { get; set; }
        Task ProcessMessage(Func<BrokeredMessage, Task> action, BrokeredMessage message);
        Task<int> ProcessMessageBatch(Func<Task<int>> action, CancellationTokenSource tokenSource);
        Task OnMessageHandlerCompleted(BrokeredMessage message, string body);
        Task OnMessageHandlerFailed(BrokeredMessage message, string body, Exception ex);
        ISubscriberPolicy AppendInnerPolicy(ISubscriberPolicy innerPolicy);

    }
}