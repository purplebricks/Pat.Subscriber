namespace Pat.Subscriber.IntegrationTests.Helpers
{
    public class MessageReceivedHandlerArgs<T>
    {
        public CapturedMessage<T> CapturedMessage { get; set; }
    }
}