using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace PB.ITOps.Messaging.PatLite
{
    public interface IMessageProcessor
    {
        Task ProcessMessage(Message message, IMessageReceiver messageReceiver);
    }
}