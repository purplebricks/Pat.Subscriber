using System.Collections.Generic;
using PB.ITOps.Messaging.DataProtection;
using PB.ITOps.Messaging.PatLite;
using PB.ITOps.Messaging.PatLite.Encryption;
using PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.MonitoringPolicy;
using PB.ITOps.Messaging.PatLite.RateLimiterPolicy;
using PB.ITOps.Messaging.PatLite.Serialiser;
using PB.ITOps.Messaging.PatLite.StructureMap4;
using PB.ITOps.Messaging.PatSender;
using PB.ITOps.Messaging.PatSender.Encryption;
using Purplebricks.StatsD.Client;
using StructureMap;

namespace TestSubscriber
{
    public class Program
    {
        static void Main()
        {
            var container = Initialize();
            
            var messagePublisher = container.GetInstance<IMessagePublisher>();

            var myEvents = new List<object>
            {
                new MyEvent1
                {
                    Message = "Test encryption"
                },
                new MyDerivedEvent2()
            };
            messagePublisher.PublishEvents(myEvents);

            var subscriber = container.GetInstance<Subscriber>();
            subscriber.Run();
        }

        public static IContainer Initialize()
        {
            var connection = "Endpoint=sb://***REMOVED***.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=***REMOVED***";
            var topicName = "pat"; 

            var subscriberConfig = new SubscriberConfiguration
            {
                ConnectionStrings = new[] { connection },
                TopicName = topicName,
                UsePartitioning = true,
                SubscriberName = "Rightmove",
                BatchSize = 100
            };
            var options = new PatLiteOptions
            {
                SubscriberConfiguration = subscriberConfig,
                GlobalPolicyBuilder = new PatLiteGlobalPolicyBuilder()
                    .AddPolicy<RateLimiterPolicy>()
                    .AddPolicy<StandardPolicy>()
                    .AddPolicy<MonitoringPolicy>()
            };
            var patSenderConfig = new PatSenderSettings
            {
                PrimaryConnection = connection,
                TopicName = topicName,
                UseDevelopmentTopic = true
            };

            StatsDConfiguration.Initialize(new StatsDConfiguration.Settings
            {
                Environment = "local",
                StatsDHost = "statsd-statsd-monitoring-tm-we-pb.trafficmanager.net",
                Tenant = "uk"
            });

            using (StatsDSender.StartTimer("IocStartup", $"Client=PatLite.{subscriberConfig.SubscriberName}"))
            {

                var container = new Container(x =>
                {
                    x.AddRegistry(new PatLiteRegistry(options));
                });

                container.Configure(x =>
                {
                    x.Scan(scanner =>
                    {
                        scanner.WithDefaultConventions();
                        scanner.AssemblyContainingType<IMessagePublisher>();
                    });
                    x.For<RateLimiterPolicyOptions>().Use(
                        new RateLimiterPolicyOptions(
                            new RateLimiterPolicyConfiguration
                            {
                                RateLimit = 100
                            })
                    );
                    x.For<DataProtectionConfiguration>().Use(new DataProtectionConfiguration
                    {
                        AccountName = "***REMOVED***",
                        AccountKey =
                            "***REMOVED***",
                        Thumbprint = "***REMOVED***"
                    });
                    x.For<IEncryptedMessagePublisher>().Use<EncryptedMessagePublisher>()
                    .Ctor<string>().Is((c) => c.GetInstance<IMessageContext>().CorrelationId);
                    x.For<IMessagePublisher>().Use<MessagePublisher>()
                        .Ctor<IDictionary<string, string>>().Is(c => null)
                        .Ctor<string>().Is((c) => c.GetInstance<IMessageContext>().CorrelationId);
                    x.For<IMessageDeserialiser>().Use(ctx => ctx.GetInstance<IMessageContext>().MessageEncrypted
                        ? new EncryptedMessageDeserialiser(ctx.GetInstance<DataProtectionConfiguration>())
                        : (IMessageDeserialiser)new NewtonsoftMessageDeserialiser());
                    x.For<PatSenderSettings>().Use(patSenderConfig);
                });

                return container;
            }
        }
    }
}
