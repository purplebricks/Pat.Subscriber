using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy;

namespace PB.ITOps.Messaging.PatLite
{
    public interface IMessageProcessor
    {
        Task ProcessMessage(BrokeredMessage message, ISubscriberPolicy globalPolicy);
    }
}