using System.Collections.Generic;
using System.Linq;
using log4net;
using Microsoft.Azure.ServiceBus.Core;

namespace Pat.Subscriber
{
    public abstract class MessageReceiverFactory
    {
        private readonly ILog _log;
        private readonly SubscriberConfiguration _config;

        protected abstract IMessageReceiver CreateMessageReceiver(
            string connectionString,
            string topicName,
            string subscriberName);

        protected MessageReceiverFactory(ILog log, SubscriberConfiguration config)
        {
            _log = log;
            _config = config;
        }

        public IList<IMessageReceiver> CreateReceivers()
        {
            var messageReceivers = new List<IMessageReceiver>();

            foreach (var messageReceiver in CreateMessageReceivers())
            {
                messageReceivers.AddRange(Enumerable.Repeat(messageReceiver, _config.ConcurrentBatches));
            }

            return messageReceivers;
        }

        private List<IMessageReceiver> CreateMessageReceivers()
        {
            var messageReceivers = new List<IMessageReceiver>();

            var clientIndex = 1;
            foreach (var connectionString in _config.ConnectionStrings)
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _log.Info($"Adding on subscription client {clientIndex} to list of source subscriptions");
                    messageReceivers.Add(CreateMessageReceiver(connectionString,
                        _config.EffectiveTopicName, _config.SubscriberName));
                }
                else
                {
                    _log.Info($"Skipping subscription client {clientIndex}, connection string is null or empty");
                }
                clientIndex++;
            }
            return messageReceivers;
        }
    }
}