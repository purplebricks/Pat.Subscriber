using PB.ITOps.Messaging.PatLite;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.StructureMap4;
using PB.ITOps.Messaging.PatSender;
using StructureMap;

namespace TestSubscriber
{
    class Program
    {
        static void Main()
        {
            var container = Initialize();

            var subscriber = container.GetInstance<Subscriber>();
            subscriber.Run();
        }

        public static IContainer Initialize()
        {
            var connection = "Endpoint=sb://***REMOVED***.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=***REMOVED***";
            var topicName = "pat2G5FKC2";

            var config = new SubscriberConfig
            {
                ConnectionStrings = new[] { connection },
                TopicName = topicName,
                UsePartitioning = true,
                SubscriberName = "Rightmove",
                BatchSize = 1
            };
            var patSenderConfig = new PatSenderSettings
            {
                PrimaryConnection = connection,
                TopicName = topicName
            };
            var container = new Container(x =>
            {
                x.AddRegistry(new PatLiteRegistry(config));
                x.Scan(scanner =>
                {
                    scanner.WithDefaultConventions();
                    scanner.AssemblyContainingType<IMessagePublisher>();
                });
                x.For<IMessagePublisher>().Use<MessagePublisher>().Ctor<string>().Is((context) => context.GetInstance<IMessageContext>().CorrelationId);
                x.For<PatSenderSettings>().Use(patSenderConfig);
            });

            return container;
        }
    }
}
