using System.Collections.Generic;
using Microsoft.Azure.ServiceBus.Core;

namespace Pat.Subscriber
{
    public interface IMessageReceiverFactory
    {
        IList<IMessageReceiver> CreateReceivers();
    }
}