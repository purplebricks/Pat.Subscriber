using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
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
                var messageBody = await GetMessageBody(message);
                        
                var correlationId = message.Properties.ContainsKey("PBCorrelationId")
                    ? message.Properties["PBCorrelationId"].ToString()
                    : Guid.NewGuid().ToString();
                var encrypted = message.Properties.ContainsKey("Encrypted") && bool.Parse(message.Properties["Encrypted"].ToString());

                var ctx = (IMessageContext)scope.GetService(typeof(IMessageContext));
                ctx.CorrelationId = correlationId;
                ctx.MessageEncrypted = encrypted;

                ProcessCustomProperties(message, ctx);

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

        private static async Task<string> GetMessageBody(BrokeredMessage message)
        {
            /*
             * This is a work around to cope with two clients, one client 
             * is publishing with a stream payload, the other is with a 
             * string payload. Once all versions of the PB.ITOps.Messaging.PatSender 
             * are on 1.5.28 or above this can be removed.
             */
            try
            {
                var clone = message.Clone();
                return clone.GetBody<string>();
            }
            catch (SerializationException)
            {
                using (var messageStream = message.GetBody<Stream>())
                {
                    using (var reader = new StreamReader(messageStream))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
        }

        private static void ProcessCustomProperties(BrokeredMessage message, IMessageContext ctx)
        {
            foreach (var messageProperty in message.Properties)
            {
                if (messageProperty.Key != "PBCorrelationId" && messageProperty.Key != "Encrypted")
                {
                    if (ctx.CustomProperties == null)
                    {
                        ctx.CustomProperties = new Dictionary<string, object>();
                    }
                    ctx.CustomProperties.Add(messageProperty.Key, messageProperty.Value);
                }
            }
        }
    }
}
