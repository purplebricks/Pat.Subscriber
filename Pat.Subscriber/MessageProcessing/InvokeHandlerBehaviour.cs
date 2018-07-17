using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.InteropExtensions;
using Pat.Subscriber.Deserialiser;
using Pat.Subscriber.MessageMapping;

namespace Pat.Subscriber.MessageProcessing
{
    /// <summary>
    /// - Completes message on handler success
    /// - No action on message failure: allow peek lock to expire, and default delivery count for dead lettering
    /// </summary>
    public class InvokeHandlerBehaviour : IMessageProcessingBehaviour
    {
        private readonly ILog _log;
        private readonly SubscriberConfiguration _config;

        public InvokeHandlerBehaviour(ILog log, SubscriberConfiguration config)
        {
            _log = log;
            _config = config;
        }

        public async Task Invoke(Func<MessageContext, Task> next, MessageContext messageContext)
        {
            var message = messageContext.Message;
            try
            {
                var messageBody = await GetMessageBody(message).ConfigureAwait(false);
                var handlerForMessageType = GetHandlerForMessageType(message);
                var messageDeserialiser = messageContext.DependencyScope.GetService<IMessageDeserialiser>();
                var messageHandler = messageContext.DependencyScope.GetService(handlerForMessageType.HandlerType);
                var typedMessage = messageDeserialiser.DeserialiseObject(messageBody, handlerForMessageType.MessageType);
            
                var handlerTask = (Task)handlerForMessageType.HandlerMethod.Invoke(messageHandler, new[] { typedMessage });
                await handlerTask.ConfigureAwait(false);
                await next(messageContext);
            }
            catch (TargetInvocationException exception)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            }
            catch (SerializationException ex)
            {
                var messageType = GetMessageType(message);
                var correlationId = GetCollelationId(message);
                await messageContext.MessageReceiver.DeadLetterAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                _log.Warn($"Unable to deserialise message body, message deadlettered. `{messageType}` correlation id `{correlationId}` on subscriber `{_config.SubscriberName}`.", ex);
            }
        }

        public virtual MessageTypeMapping GetHandlerForMessageType(Message message)
        {
            var messageTypeString = message.UserProperties["MessageType"].ToString();
            var handlerForMessageType = MessageMapper.GetHandlerForMessageType(messageTypeString);
            return handlerForMessageType;
        }


        private async Task<string> GetMessageBody(Message message)
        {
            /*
                * This is a work around to cope with two clients, one client 
                * is publishing with a stream payload, the other is with a 
                * string payload. Once all versions of the Pat.Subscriber.PatSender 
                * are on 2.0.33 or above this can be removed.
                * 
                * Yes, this is awful.
                */
            try
            {
                return MessageAsStringReaderStrategy(message.Clone());
            }
            catch (SerializationException)
            {
                try
                {
                    return await MessageAsStreamReaderStrategy(message.Clone());
                }
                catch (SerializationException)
                {
                    return MessageAsUtf8ByteArrayReaderStrategy(message.Clone());
                }
            }
        }

        private static string GetMessageType(Message message)
        {
            return message.UserProperties.ContainsKey("MessageType")
                ? message.UserProperties["MessageType"].ToString()
                : "Unknown Message Type";
        }

        private static string GetCollelationId(Message message)
        {
            return message.UserProperties.ContainsKey("PBCorrelationId")
                ? message.UserProperties["PBCorrelationId"].ToString()
                : "null";
        }

        private static string MessageAsStringReaderStrategy(Message message)
        {
            return message.GetBody<string>();
        }

        private static async Task<string> MessageAsStreamReaderStrategy(Message message)
        {
            using (var messageStream = message.GetBody<Stream>())
            {
                using (var reader = new StreamReader(messageStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        private static string MessageAsUtf8ByteArrayReaderStrategy(Message message)
        {
            return Encoding.UTF8.GetString(message.Body);
        }
    }
}