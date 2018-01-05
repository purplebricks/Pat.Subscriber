using System;
using System.Collections.Generic;

namespace PB.ITOps.Messaging.PatLite.IoC
{
    public class MessageContext : IMessageContext
    {
        public string CorrelationId { get; set; }
        public bool MessageEncrypted { get; set; }
        public bool Synthetic { get; set; }
        public string DomainUnderTest { get; set; }
        public IDictionary<string, object> CustomProperties { get; set; }
        public string MessageId { get; set; }
        public DateTime MessageEnqueuedTimeUtc { get; set; }
    }
}
