using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Pat.Sender;
using Pat.Subscriber.IntegrationTests.Helpers;
using StructureMap;

namespace Pat.Subscriber.IntegrationTests.DependencyResolution
{
    public static class StructureMapExtensions
    {
        public static void SetupTestMessage(this IContainer container, string correlationId)
        {
            var messageReceiver = Substitute.For<IMessageReceiver>();
            messageReceiver.ReceiveAsync(0, TimeSpan.MaxValue).ReturnsForAnyArgs(info =>
                new List<Message>()
                {
                    MessageHelper.GenerateMessage(new TestEvent(), new MessageProperties(correlationId))
                });

            container.Configure(x =>
            {
                x.For<IMessageReceiverFactory>().Use(context => new FakeMessageReceiverFactory(
                    context.GetInstance<ILogger<FakeMessageReceiverFactory>>(),
                    context.GetInstance<SubscriberConfiguration>(),
                    messageReceiver));
                x.For<MessageWaiter<TestEvent>>().Use(context => new MessageWaiter<TestEvent>(
                    context.GetInstance<MessageReceivedNotifier<TestEvent>>(),
                    capturedEvent => capturedEvent.CorrelationId == correlationId));
            });
        }
    }
}