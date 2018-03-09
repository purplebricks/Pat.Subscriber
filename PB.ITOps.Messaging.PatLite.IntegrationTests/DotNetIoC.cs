using System;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.DataProtection;
using PB.ITOps.Messaging.PatLite.Deserialiser;
using PB.ITOps.Messaging.PatLite.Encryption;
using PB.ITOps.Messaging.PatLite.MonitoringPolicy;
using PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution;
using PB.ITOps.Messaging.PatSender;
using PB.ITOps.Messaging.PatSender.Encryption;
using PB.ITOps.Messaging.PatSender.MessageGeneration;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{

    public class DotNetIoC
    {
        public static IServiceProvider Initialize(IConfigurationRoot configuration)
        {
            var senderSettings = new PatSenderSettings();
            configuration.GetSection("PatLite:Sender").Bind(senderSettings);

            var subscriberConfiguration = new SubscriberConfiguration();
            configuration.GetSection("PatLite:Subscriber").Bind(subscriberConfiguration);

            var statisticsConfiguration = new StatisticsReporterConfiguration();
            configuration.GetSection("StatsD").Bind(statisticsConfiguration);

            var dataProtectionConfiguration = new DataProtectionConfiguration();
            configuration.GetSection("DataProtection").Bind(dataProtectionConfiguration);

            InitLogger();
            var serviceProvider = new ServiceCollection()
                .AddSingleton(senderSettings)
                .AddSingleton(subscriberConfiguration)
                .AddSingleton(statisticsConfiguration)
                .AddSingleton(dataProtectionConfiguration)
                .AddSingleton<IMessageGenerator, MessageGenerator>()
                .AddSingleton<CapturedEvents>()
                .AddTransient<IEncryptedMessagePublisher>(
                    provider => new EncryptedMessagePublisher(
                        provider.GetRequiredService<IMessageSender>(),
                        provider.GetRequiredService<DataProtectionConfiguration>(),
                        new MessageProperties(Guid.NewGuid().ToString())))
                .AddTransient<IMessagePublisher>(
                    provider => new MessagePublisher(
                        provider.GetRequiredService<IMessageSender>(),
                        provider.GetRequiredService<IMessageGenerator>(),
                        new MessageProperties(Guid.NewGuid().ToString())))
                .AddSingleton(LogManager.GetLogger("IntegrationLogger"))
                .AddTransient<IMessageSender, MessageSender>()
                .AddTransient<IStatisticsReporter, StatisticsReporter>()
                .AddPatLite(new PatLiteOptions
                {
                    MessageDeserialiser = provider => provider.GetService<MessageContext>().MessageEncrypted
                    ? new EncryptedMessageDeserialiser(provider.GetService<DataProtectionConfiguration>())
                    : (IMessageDeserialiser)new NewtonsoftMessageDeserialiser(),
                    SubscriberConfiguration = subscriberConfiguration
                })
                .AddHandlersFromAssemblyContainingType<DotNetIoC>()
                .BuildServiceProvider();

            return serviceProvider;
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