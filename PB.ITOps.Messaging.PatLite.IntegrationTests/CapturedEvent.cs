namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class CapturedEvent<T>
    {
        public T Event { get; set; }
        public string CorrelationId { get; set; }
    }
}