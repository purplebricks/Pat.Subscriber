using System;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pat.DataProtection;
using Pat.Sender;
using Pat.Sender.DataProtectionEncryption;
using Pat.Sender.MessageGeneration;
using Pat.Subscriber.DataProtectionDecryption;
using Pat.Subscriber.DataProtectionDecryption.NetCoreDependencyResolution;
using Pat.Subscriber.Deserialiser;
using Pat.Subscriber.IntegrationTests.Helpers;
using Pat.Subscriber.Telemetry.StatsD;
using IMessageSender = Pat.Sender.IMessageSender;
using MessageSender = Pat.Sender.MessageSender;

namespace Pat.Subscriber.IntegrationTests.DependencyResolution
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