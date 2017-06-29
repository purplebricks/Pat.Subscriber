using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite
{
    public interface IMessageProcessor
    {
        Task ProcessMessage(BrokeredMessage message);
    }
}