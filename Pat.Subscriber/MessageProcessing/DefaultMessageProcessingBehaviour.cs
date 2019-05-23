using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Pat.Subscriber.MessageProcessing
{
    /// <summary>
    /// - Completes message on handler success
    /// - No action on message failure: allow peek lock to expire, and default delivery count for dead lettering
    /// </summary>
    public class DefaultMessageProcessingBehaviour : IMessageProcessingBehaviour
    {
        private readonly ILogger _log;
        private readonly SubscriberConfiguration _config;

        public DefaultMessageProcessingBehaviour(ILogger<DefaultMessageProcessingBehaviour> log, SubscriberConfiguration config)
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
                _log.LogDebug($"{_config.SubscriberName} Success Handling Message with correlation id `{GetCorrelationId(message)}`: {message.ContentType}");
            }
            catch (SerializationException ex)
            {
                var messageType = GetMessageType(message);
                var correlationId = GetCorrelationId(message);
                await messageContext.MessageReceiver.DeadLetterAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                _log.LogWarning(ex, $"Unable to deserialise message body, message deadlettered. `{messageType}` correlation id `{correlationId}` on subscriber `{_config.SubscriberName}`.");
            }
            catch (Exception ex)
            {
                await HandleException(ex, message).ConfigureAwait(false);
            }
        }

        protected virtual Task HandleException(Exception exception, Message message)
        {
            _log.LogError(exception, $"Message with with correlation id `{GetCorrelationId(message)}` failed: {exception.Message}");
            return Task.CompletedTask;
        }

        protected string GetMessageType(Message message)
        {
            return message.UserProperties.ContainsKey("MessageType")
                ? message.UserProperties["MessageType"].ToString()
                : "Unknown Message Type";
        }

        protected string GetCorrelationId(Message message)
        {
            return message.UserProperties.ContainsKey("PBCorrelationId")
                ? message.UserProperties["PBCorrelationId"].ToString()
                : "null";
        }
    }
}