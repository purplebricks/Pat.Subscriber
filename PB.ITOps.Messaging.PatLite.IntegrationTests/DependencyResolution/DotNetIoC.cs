using System;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.DataProtection;
using PB.ITOps.Messaging.PatLite.Deserialiser;
using PB.ITOps.Messaging.PatLite.Encryption;
using PB.ITOps.Messaging.PatLite.IntegrationTests.Helpers;
using PB.ITOps.Messaging.PatLite.MonitoringPolicy;
using PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution;
using PB.ITOps.Messaging.PatSender;
using PB.ITOps.Messaging.PatSender.Encryption;
using PB.ITOps.Messaging.PatSender.MessageGeneration;
using IMessageSender = PB.ITOps.Messaging.PatSender.IMessageSender;
using MessageSender = PB.ITOps.Messaging.PatSender.MessageSender;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests.DependencyResolution
{

    public class DotNetIoC
    {
        public static IServiceCollection Initialize(IConfigurationRoot configuration)
        {
            var senderSettings = new PatSenderSettings();
            configuration.GetSection("PatLite:Sender").Bind(senderSettings);

            var subscriberConfiguration = new SubscriberConfiguration();
            configuration.GetSection("PatLite:Subscriber").Bind(subscriberConfiguration);

            var statisticsConfiguration = new StatisticsReporterConfiguration();
            configuration.GetSection("StatsD").Bind(statisticsConfiguration);

            var dataProtectionConfiguration = new DataProtectionConfiguration();
            configuration.GetSection("DataProtection").Bind(dataProtectionConfiguration);

            var loggerName = "IntegrationLogger";
            Logging.InitLogger(loggerName);
            var serviceCollection = new ServiceCollection()
                .AddSingleton(senderSettings)
                .AddSingleton(subscriberConfiguration)
                .AddSingleton(statisticsConfiguration)
                .AddSingleton(dataProtectionConfiguration)
                .AddSingleton<IMessageGenerator, MessageGenerator>()
                .AddSingleton<MessageReceivedNotifier<TestEvent>>()
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
                .AddSingleton(LogManager.GetLogger(loggerName, loggerName))
                .AddTransient<IMessageSender, MessageSender>()
                .AddTransient<IStatisticsReporter, StatisticsReporter>()
                .AddPatLite(new PatLiteOptions
                {
                    MessageDeserialiser = provider => provider.GetService<MessageContext>().MessageEncrypted
                        ? new EncryptedMessageDeserialiser(provider.GetService<DataProtectionConfiguration>())
                        : (IMessageDeserialiser) new NewtonsoftMessageDeserialiser(),
                    SubscriberConfiguration = subscriberConfiguration
                })
                .AddHandlersFromAssemblyContainingType<DotNetIoC>();

            return serviceCollection;
        }
    }
}