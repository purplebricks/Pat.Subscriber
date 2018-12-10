using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using NSubstitute;
using Pat.Sender;
using Pat.Sender.Correlation;
using Pat.Sender.MessageGeneration;
using Pat.Subscriber.IntegrationTests.DependencyResolution;
using IMessageSender = Pat.Sender.IMessageSender;

namespace Pat.Subscriber.IntegrationTests.Helpers
{
    public class TestMessageSender
    {
        private readonly IGenericServiceProvider _serviceProvider;
        private readonly bool _integrationTest;

        public TestMessageSender(IGenericServiceProvider serviceProvider, bool integrationTest = false)
        {
            _serviceProvider = serviceProvider;
            _integrationTest = integrationTest;
        }

        public Task<MessageWaiter<T>> PublishMessage<T>(T testMessage, MessageProperties messageProperties, string domainUnderTest = null, IMessageGenerator messageGenerator = null)
        {
            return _integrationTest
                ? PublishRealMessage(testMessage, messageProperties, messageGenerator)
                : PublishFakeMessage(testMessage, messageProperties, messageGenerator);
        }

        public Task<MessageWaiter<T>> PublishMessage<T>(T testMessage, string correlationId, string domainUnderTest = null, IMessageGenerator messageGenerator = null)
        {
            return _integrationTest
                ? PublishRealMessage(testMessage, correlationId, messageGenerator)
                : PublishFakeMessage(testMessage, correlationId, messageGenerator);
        }

        private Task<MessageWaiter<T>> PublishRealMessage<T>(T testMessage, string correlationId, IMessageGenerator messageGenerator)
        {
            return PublishRealMessage(testMessage, new MessageProperties(correlationId), messageGenerator);
        }

        private async Task<MessageWaiter<T>> PublishRealMessage<T>(T testMessage, MessageProperties messageProperties, IMessageGenerator messageGenerator)
        {
            messageGenerator = messageGenerator ?? new MessageGenerator();
            var messagePublisher = new MessagePublisher(
                _serviceProvider.GetService<IMessageSender>(),
                messageGenerator,
                new MessageProperties(_serviceProvider.GetService<ICorrelationIdProvider>()));

            var messageWaiter = new MessageWaiter<T>(
                _serviceProvider.GetService<MessageReceivedNotifier<T>>(),
                capturedEvent => capturedEvent.CorrelationId == messageProperties.CorrelationIdProvider.CorrelationId);

            await messagePublisher.PublishEvent(testMessage, messageProperties).ConfigureAwait(false);

            return messageWaiter;
        }

        private Task<MessageWaiter<T>> PublishFakeMessage<T>(T testMessage, string correlationId, IMessageGenerator messageGenerator)
        {
            return PublishFakeMessage(testMessage, new MessageProperties(correlationId), messageGenerator);
        }

        private Task<MessageWaiter<T>> PublishFakeMessage<T>(T testMessage, MessageProperties messageProperties, IMessageGenerator messageGenerator)
        {
            messageGenerator = messageGenerator ?? new MessageGenerator();
            var messageReceiver = _serviceProvider.GetService<IMessageReceiver>();
            messageReceiver.ReceiveAsync(0, TimeSpan.MaxValue).ReturnsForAnyArgs(info =>
                new List<Message>()
                {
                    MessageHelper.GenerateMessage(testMessage, messageProperties, messageGenerator)
                });

            var messageWaiter = new MessageWaiter<T>(
                _serviceProvider.GetService<MessageReceivedNotifier<T>>(),
                capturedEvent => capturedEvent.CorrelationId == messageProperties.CorrelationIdProvider.CorrelationId);

            return Task.FromResult(messageWaiter);
        }
    }
}