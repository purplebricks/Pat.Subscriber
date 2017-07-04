using System;
using PB.ITOps.Messaging.PatLite;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.MonitoringPolicy;
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
            var topicName = "pat" + Environment.MachineName; 

            var subscriberConfig = new SubscriberConfig
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
            var monitoringConfig = new MonitoringConfig
            {
                StatsDHost = "statsd-statsd-monitoring-tm-we-pb.trafficmanager.net",
                StatsDPort = 8125,
                Environment = "local"
            };
            var container = new Container(x =>
            {
                x.AddRegistry(new PatLiteRegistry(subscriberConfig, monitoringConfig));
            });

            container.Configure(x =>
            {
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
