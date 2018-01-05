using System;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.MessageProcessingPolicy
{
    /// <summary>
    /// - Completes message on handler success
    /// - No action on message failure: allow peek lock to expire, and default delivery count for dead lettering
    /// </summary>
    public class DefaultMessageProcessingPolicy : BaseMessageProcessingPolicy
    {
        private readonly ILog _log;
        private readonly SubscriberConfiguration _config;

        public DefaultMessageProcessingPolicy(ILog log, SubscriberConfiguration config)
        {
            _log = log;
            _config = config;
        }

        protected override Task<bool> MessageHandlerCompleted(BrokeredMessage message, string body)
        {
            _log.Info($"{_config.SubscriberName} Success Handling Message {message.SequenceNumber}: {message.ContentType}");
            message.Complete();
            return Task.FromResult(true);
        }

        protected override Task<bool> MessageHandlerFailed(BrokeredMessage message, string body, Exception ex)
        {
            _log.Info($"Message {message.SequenceNumber} failed", ex);
            return Task.FromResult(true);
        }

        protected override Task<bool> MessageHandlerStarted(BrokeredMessage message, string body)
        {
            _log.Info($"{_config.SubscriberName} Message Handling Started {message.SequenceNumber}: {message.ContentType}");
            return Task.FromResult(true);
        }
    }
}