using System;
using System.Collections.Generic;
using PB.ITOps.Messaging.PatLite;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.MonitoringPolicy;
using PB.ITOps.Messaging.PatLite.StructureMap4;
using PB.ITOps.Messaging.PatSender;
using Purplebricks.StatsD.Client;
using StructureMap;

namespace TestSubscriber
{
    class Program
    {
        static void Main()
        {
            var container = Initialize();

            var messagePublisher = container.GetInstance<IMessagePublisher>();

            var myEvents = new List<object>
            {
                new MyEvent1(),
                new MyDerivedEvent2()
            };
            messagePublisher.PublishEvents(myEvents);

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
                BatchSize = 100
            };
            var patSenderConfig = new PatSenderSettings
            {
                PrimaryConnection = connection,
                TopicName = topicName,
                UseDevelopmentTopic = false
            };

            StatsDConfiguration.Initialize(new StatsDConfiguration.Settings
            {
                Environment = "local",
                StatsDHost = "statsd-statsd-monitoring-tm-we-pb.trafficmanager.net",
                Tenant = "uk"
            });

            var container = new Container(x =>
            {
                x.AddRegistry(new PatLiteRegistry(subscriberConfig));
            });

            container.Configure(x =>
            {
                x.Scan(scanner =>
                {
                    scanner.WithDefaultConventions();
                    scanner.AssemblyContainingType<IMessagePublisher>();
                });
                x.For<IMessagePublisher>().Use<MessagePublisher>().Ctor<string>().Is((c) => c.GetInstance<IMessageContext>().CorrelationId);
                x.For<PatSenderSettings>().Use(patSenderConfig);
            });

            return container;
        }
    }
}
