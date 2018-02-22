using System;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Configuration;
using PB.ITOps.Messaging.DataProtection;
using PB.ITOps.Messaging.PatLite.Deserialiser;
using PB.ITOps.Messaging.PatLite.Encryption;
using PB.ITOps.Messaging.PatLite.MonitoringPolicy;
using PB.ITOps.Messaging.PatLite.StructureMap4;
using PB.ITOps.Messaging.PatSender;
using PB.ITOps.Messaging.PatSender.Correlation;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
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

            InitLogger();
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
                x.For<DataProtectionConfiguration>().Use(dataProtectionConfiguration);
            });

            return container;
        }

        private static void InitLogger()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();
            var tracer = new TraceAppender();
            var patternLayout = new PatternLayout();

            patternLayout.ConversionPattern = "%d [%t] %-5p %m%n";
            patternLayout.ActivateOptions();

            tracer.Layout = patternLayout;
            tracer.ActivateOptions();
            hierarchy.Root.AddAppender(tracer);

            var roller = new RollingFileAppender();
            roller.Layout = patternLayout;
            roller.AppendToFile = true;
            roller.RollingStyle = RollingFileAppender.RollingMode.Size;
            roller.MaxSizeRollBackups = 4;
            roller.MaximumFileSize = "100KB";
            roller.StaticLogFileName = true;
            roller.File = "IntegrationLogger.txt";
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            hierarchy.Root.Level = Level.All;
            hierarchy.Configured = true;
        }
    }
}
