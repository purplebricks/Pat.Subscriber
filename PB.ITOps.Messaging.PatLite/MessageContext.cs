using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using PB.ITOps.Messaging.PatLite.IoC;

namespace PB.ITOps.Messaging.PatLite
{
    public class MessageContext
    {
        public string CorrelationId { get; set; }
        public bool MessageEncrypted { get; set; }
        public bool Synthetic { get; set; }
        public string DomainUnderTest { get; set; }
        public IMessageReceiver MessageReceiver { get; set; }
        public Message Message { get; set; }
        public IMessageDependencyScope DependencyScope { get; set; }
    }
}
