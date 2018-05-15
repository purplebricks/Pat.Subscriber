namespace Pat.Subscriber.IntegrationTests.Helpers
{
    public class CapturedMessage<T>
    {
        public T Message { get; set; }
        public string CorrelationId { get; set; }
    }
}