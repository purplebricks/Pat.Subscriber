using System;
using System.Collections.Generic;
using log4net;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using NSubstitute;
using PB.ITOps.Messaging.PatLite.IntegrationTests.Helpers;
using PB.ITOps.Messaging.PatSender;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests.DependencyResolution
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
                x.For<MessageReceiverFactory>().Use(context => new FakeMessageReceiverFactory(
                    context.GetInstance<ILog>(),
                    context.GetInstance<SubscriberConfiguration>(),
                    messageReceiver));
                x.For<MessageWaiter<TestEvent>>().Use(context => new MessageWaiter<TestEvent>(
                    context.GetInstance<MessageReceivedNotifier<TestEvent>>(),
                    capturedEvent => capturedEvent.CorrelationId == correlationId));
            });
        }
    }
}