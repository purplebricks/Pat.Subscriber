using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using log4net;
using Microsoft.Azure.ServiceBus;

namespace Pat.Subscriber.MessageProcessing
{
    /// <summary>
    /// - Completes message on handler success
    /// - No action on message failure: allow peek lock to expire, and default delivery count for dead lettering
    /// </summary>
    public class DefaultMessageProcessingBehaviour : IMessageProcessingBehaviour
    {
        private readonly ILog _log;
        private readonly SubscriberConfiguration _config;

        public DefaultMessageProcessingBehaviour(ILog log, SubscriberConfiguration config)
        {
            _log = log;
            _config = config;
        }

        public async Task Invoke(Func<MessageContext, Task> next, MessageContext messageContext)
        {
            var message = messageContext.Message;
            try
            {
                await next(messageContext).ConfigureAwait(false);
                await messageContext.MessageReceiver.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                _log.Debug($"{_config.SubscriberName} Success Handling Message {message.MessageId} correlation id `{GetCollelationId(message)}`: {message.ContentType}");
            }
            catch (SerializationException ex)
            {
                var messageType = GetMessageType(message);
                var correlationId = GetCollelationId(message);
                await messageContext.MessageReceiver.DeadLetterAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                _log.Warn($"Unable to deserialise message body, message deadlettered. `{messageType}` correlation id `{correlationId}` on subscriber `{_config.SubscriberName}`.", ex);
            }
            catch (Exception ex)
            {
                await HandleUnhandledException(ex, messageContext);
            }
        }

        protected virtual Task HandleUnhandledException(Exception ex, MessageContext messageContext)
        {
            _log.Info($"Message {messageContext.Message.MessageId} failed", ex);
#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
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
    }
}