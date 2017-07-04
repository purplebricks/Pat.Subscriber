using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using PB.ITOps.Messaging.PatLite.Policy;

namespace PB.ITOps.Messaging.PatLite
{
    public interface IMessageProcessor
    {
        Task ProcessMessage(BrokeredMessage message, ISubscriberPolicy policy);
    }
}