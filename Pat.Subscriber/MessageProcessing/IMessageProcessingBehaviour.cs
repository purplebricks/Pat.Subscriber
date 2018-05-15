using System;
using System.Threading.Tasks;

namespace Pat.Subscriber.MessageProcessing
{
    public interface IMessageProcessingBehaviour
    {
        Task Invoke(Func<MessageContext, Task> next, MessageContext messageContext);
    }
}