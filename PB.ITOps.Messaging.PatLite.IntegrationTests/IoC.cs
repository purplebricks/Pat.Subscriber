using System;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution;
using PB.ITOps.Messaging.PatSender;
using PB.ITOps.Messaging.PatSender.MessageGeneration;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class IoC
    {
        public static IServiceProvider Initialize(IConfigurationRoot configuration)
        {
            var senderSettings = new PatSenderSettings();
            configuration.GetSection("PatLite:Sender").Bind(senderSettings);

            var subscriberSettings = new SubscriberConfiguration();
            configuration.GetSection("PatLite:Subscriber").Bind(subscriberSettings);

            var serviceProvider = new ServiceCollection()
                .AddSingleton(senderSettings)
                .AddSingleton(subscriberSettings)
                .AddSingleton<IMessageGenerator, MessageGenerator>()
                .AddTransient<IMessagePublisher>(
                    provider => new MessagePublisher(
                        provider.GetRequiredService<IMessageSender>(),
                        provider.GetRequiredService<IMessageGenerator>(),
                        new MessageProperties(Guid.NewGuid().ToString())))
                .AddSingleton(LogManager.GetLogger("Log"))
                .AddTransient<IMessageSender, MessageSender>()
                .AddPatLite()
                .AddHandlersFromAssemblyContainingType<IoC>()
                .BuildServiceProvider();

            return serviceProvider;
        }
    }
}