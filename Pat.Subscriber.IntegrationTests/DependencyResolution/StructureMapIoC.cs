using System;
using log4net;
using Microsoft.Extensions.Configuration;
using PB.ITOps.Messaging.DataProtection;
using PB.ITOps.Messaging.PatLite.Deserialiser;
using PB.ITOps.Messaging.PatLite.Encryption;
using PB.ITOps.Messaging.PatLite.IntegrationTests.Helpers;
using PB.ITOps.Messaging.PatLite.MonitoringPolicy;
using PB.ITOps.Messaging.PatLite.StructureMap4;
using PB.ITOps.Messaging.PatSender;
using PB.ITOps.Messaging.PatSender.Correlation;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests.DependencyResolution
{
    public class StructureMapIoC
    {
        public static IContainer Initialize(IConfigurationRoot configuration)
        {
            var senderSettings = new PatSenderSettings();
            configuration.GetSection("PatLite:Sender").Bind(senderSettings);

            var subscriberConfiguration = new SubscriberConfiguration();
            configuration.GetSection("PatLite:Subscriber").Bind(subscriberConfiguration);

            var statisticsConfiguration = new StatisticsReporterConfiguration();
            configuration.GetSection("StatsD").Bind(statisticsConfiguration);

            var dataProtectionConfiguration = new DataProtectionConfiguration();
            configuration.GetSection("DataProtection").Bind(dataProtectionConfiguration);

            var statsReporter = new StatisticsReporter(statisticsConfiguration);

            var loggerName = "IntegrationLogger";
            Logging.InitLogger(loggerName);
            var container = new Container(x =>
            {
                x.AddRegistry(new PatLiteRegistry(new PatLiteOptions
                {
                    SubscriberConfiguration = subscriberConfiguration
                }));
            });

            container.Configure(x =>
            {
                x.Scan(scanner =>
                {
                    scanner.WithDefaultConventions();
                    scanner.AssemblyContainingType<IMessagePublisher>();
                });

                x.For<IStatisticsReporter>().Use(statsReporter);
                x.For<ICorrelationIdProvider>().Use(new LiteralCorrelationIdProvider(Guid.NewGuid().ToString()));
                x.For<IMessageDeserialiser>().Use(ctx => ctx.GetInstance<MessageContext>().MessageEncrypted
                    ? new EncryptedMessageDeserialiser(ctx.GetInstance<DataProtectionConfiguration>())
                    : (IMessageDeserialiser)new NewtonsoftMessageDeserialiser());
                x.For<PatSenderSettings>().Use(senderSettings);
                x.For<MessageReceivedNotifier<TestEvent>>().Use(new MessageReceivedNotifier<TestEvent>());
                x.For<DataProtectionConfiguration>().Use(dataProtectionConfiguration);
                x.For<ILog>().Use(LogManager.GetLogger(loggerName, loggerName));
            });

            return container;
        }
    }
}
