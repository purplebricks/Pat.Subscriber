namespace PB.ITOps.Messaging.PatLite.IntegrationTests.Helpers
{
    public class MessageReceivedHandlerArgs<T>
    {
        public CapturedMessage<T> CapturedMessage { get; set; }
    }
}