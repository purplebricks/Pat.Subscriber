using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PB.ITOps.Messaging.PatLite.IntegrationTests.Helpers;
using PB.ITOps.Messaging.PatSender;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests.DependencyResolution
{
    public static class DotNetIoCExtensions
    {
        public static IServiceCollection SetupTestMessage(this IServiceCollection serviceCollection, string correlationId)
        {
            var messageReceiver = Substitute.For<IMessageReceiver>();
            messageReceiver.ReceiveAsync(0, TimeSpan.MaxValue).ReturnsForAnyArgs(info =>
                new List<Message>()
                {
                    MessageHelper.GenerateMessage(new TestEvent(), new MessageProperties(correlationId))
                });

            serviceCollection.Remove(serviceCollection.First(s =>
                s.Lifetime == ServiceLifetime.Singleton && s.ServiceType == typeof(MessageReceiverFactory)));

            serviceCollection
                .AddSingleton(messageReceiver)
                .AddSingleton<MessageReceiverFactory>(provider => new FakeMessageReceiverFactory(
                    provider.GetService<ILog>(),
                    provider.GetService<SubscriberConfiguration>(),
                    provider.GetService<IMessageReceiver>()))
                .AddSingleton(provider => new MessageWaiter<TestEvent>(
                    provider.GetService<MessageReceivedNotifier<TestEvent>>(),
                    capturedEvent => capturedEvent.CorrelationId == correlationId));

            return serviceCollection;
        }
    }
}