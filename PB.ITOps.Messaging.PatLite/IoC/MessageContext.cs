namespace PB.ITOps.Messaging.PatLite.IoC
{
    public class MessageContext: IMessageContext
    {
        public string CorrelationId { get; set; }
    }
}
