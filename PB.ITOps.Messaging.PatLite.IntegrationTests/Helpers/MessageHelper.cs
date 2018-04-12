using System;
using Microsoft.Azure.ServiceBus;
using PB.ITOps.Messaging.PatSender;
using PB.ITOps.Messaging.PatSender.Extensions;
using PB.ITOps.Messaging.PatSender.MessageGeneration;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests.Helpers
{
    public class MessageHelper
    {
        public static Message GenerateMessage(object payload, MessageProperties messageSpecificProperties, IMessageGenerator messageGenerator = null)
        {
            messageGenerator= messageGenerator ?? new MessageGenerator();
            var message = messageGenerator.GenerateMessage(payload);

            var messageType = payload.GetType();
            message.MessageId = Guid.NewGuid().ToString();
            message.ContentType = messageType.SimpleQualifiedName();
            message.UserProperties["MessageType"] = messageType.FullName;

            message.PopulateCorrelationId(
                messageSpecificProperties?.CorrelationIdProvider.CorrelationId);

            message.AddProperties(messageSpecificProperties?.CustomProperties);

            return message;
        }
    }
}