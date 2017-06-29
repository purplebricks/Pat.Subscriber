namespace PB.ITOps.Messaging.PatLite
{
    public class SubscriberConfig
    {
        public string[] ConnectionStrings { get; set; }
        public string TopicName { get; set; }
        public bool UsePartitioning { get; set; }
    }
}