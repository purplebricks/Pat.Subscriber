using System.Collections.Generic;

namespace PB.ITOps.Messaging.PatLite.IoC
{
    public interface IMessageContext
    {
        string CorrelationId { get; set; }
        bool MessageEncrypted { get; set; }
        IDictionary<string, object> CustomProperties { get; set; }
    }
}