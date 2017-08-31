using System;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus.Messaging;
using PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.MessageMapping;
using PB.ITOps.Messaging.PatLite.MessageProcessingPolicy;
using PB.ITOps.Messaging.PatLite.Serialiser;

namespace PB.ITOps.Messaging.PatLite
{
    public class MessageProcessor: IMessageProcessor
    {
        private readonly IMessageDependencyResolver _messageDependencyResolver;

        public MessageProcessor(IMessageDependencyResolver messageDependencyResolver)
        {
            _messageDependencyResolver = messageDependencyResolver;
        }

        public async Task ProcessMessage(BrokeredMessage message, ISubscriberPolicy globalPolicy)
        {
            using (var scope = _messageDependencyResolver.BeginScope())
            {
                var messageTypeString = message.Properties["MessageType"].ToString();
                var messageBody = message.GetBody<string>();
                        
                var ctx = (IMessageContext)scope.GetService(typeof(IMessageContext));
                var correlationId = message.Properties.ContainsKey("PBCorrelationId")
                    ? message.Properties["PBCorrelationId"].ToString()
                    : Guid.NewGuid().ToString();
                var encrypted = message.Properties.ContainsKey("Encrypted") && bool.Parse(message.Properties["Encrypted"].ToString());
                ctx.CorrelationId = correlationId;
                ctx.MessageEncrypted = encrypted;
                LogicalThreadContext.Properties["CorrelationId"] = correlationId;

                var messageProcessingPolicy = (IMessageProcessingPolicy)scope.GetService(typeof(IMessageProcessingPolicy));
                var messageDeserialiser = (IMessageDeserialiser)scope.GetService(typeof(IMessageDeserialiser));

                var handlerForMessageType = MessageMapper.GetHandlerForMessageType(messageTypeString);
                var messageHandler = scope.GetService(handlerForMessageType.HandlerType);

                try
                {
                    var typedMessage = messageDeserialiser.DeserialiseObject(messageBody, handlerForMessageType.MessageType);

                    await (Task)handlerForMessageType.HandlerMethod.Invoke(messageHandler, new [] { typedMessage });
                        
                    await messageProcessingPolicy.OnMessageHandlerCompleted(message, messageBody);
                    await globalPolicy.OnMessageHandlerCompleted(message, messageBody);
                }
                catch (Exception ex)
                {
                    await messageProcessingPolicy.OnMessageHandlerFailed(message, messageBody, ex);
                    await globalPolicy.OnMessageHandlerFailed(message, messageBody, ex);
                }
            }
        }
    }
}
