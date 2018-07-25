using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Pat.Sender;
using Pat.Subscriber.IntegrationTests.Helpers;

namespace Pat.Subscriber.IntegrationTests.DependencyResolution
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
                    provider.GetService<ILogger>(),
                    provider.GetService<SubscriberConfiguration>(),
                    provider.GetService<IMessageReceiver>()))
                .AddSingleton(provider => new MessageWaiter<TestEvent>(
                    provider.GetService<MessageReceivedNotifier<TestEvent>>(),
                    capturedEvent => capturedEvent.CorrelationId == correlationId));

            return serviceCollection;
        }
    }
}