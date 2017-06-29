using PB.ITOps.Messaging.PatLite;
using PB.ITOps.Messaging.PatSender;
using TestSubscriber.DependencyResolution;

namespace TestSubscriber
{
    class Program
    {
        static void Main(string[] args)
        {
            var connection = "Endpoint=sb://***REMOVED***.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=***REMOVED***";
            var topicName = "pat2G5FKC2";

            var subscriberConfig = new SubscriberConfig
            {
                ConnectionStrings = new[] { connection },
                TopicName = topicName,
                UsePartitioning = true,
                SubscriberName = "Rightmove"
            };
            var patSenderConfig = new PatSenderSettings
            {
                PrimaryConnection = connection,
                TopicName = topicName
            };

            var container = IoC.Initialize(subscriberConfig, patSenderConfig);

            var subscriber = container.GetInstance<Subscriber>();
            subscriber.Run();
        }
    }
}
