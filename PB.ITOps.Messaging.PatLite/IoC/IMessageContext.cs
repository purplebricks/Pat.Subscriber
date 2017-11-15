using System.Collections.Generic;

namespace PB.ITOps.Messaging.PatLite.IoC
{
    public interface IMessageContext
    {
        /// <summary>
        /// Id linking message back to originating action
        /// </summary>
        string CorrelationId { get; set; }
        /// <summary>
        /// Indicates if the message body is encrypted
        /// </summary>
        bool MessageEncrypted { get; set; }
        /// <summary>
        /// Indicates if the message is test data
        /// </summary>
        bool Synthetic { get; set; }
        /// <summary>
        /// Used in conjunction with the Synthetic message property to scope a message
        /// Only handlers within the scope of the domain will subscribe and receive synthetic messages
        /// </summary>
        string DomainUnderTest { get; set; }
        IDictionary<string, object> CustomProperties { get; set; }
    }
}