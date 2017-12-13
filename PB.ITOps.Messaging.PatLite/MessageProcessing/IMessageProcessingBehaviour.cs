using System;
using System.Threading.Tasks;

namespace PB.ITOps.Messaging.PatLite.MessageProcessing
{
    public interface IMessageProcessingBehaviour
    {
        Task Invoke(Func<MessageContext, Task> next, MessageContext messageContext);
    }
}