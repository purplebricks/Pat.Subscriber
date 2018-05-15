using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Pat.Subscriber
{
    public interface IMessageProcessor
    {
        Task ProcessMessage(Message message, IMessageReceiver messageReceiver);
    }
}