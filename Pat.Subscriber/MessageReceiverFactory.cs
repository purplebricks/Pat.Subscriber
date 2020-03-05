using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Logging;

namespace Pat.Subscriber
{
    public abstract class MessageReceiverFactory : IMessageReceiverFactory
    {
        private readonly ILogger _log;
        private readonly SubscriberConfiguration _config;

        protected abstract IMessageReceiver CreateMessageReceiver(
            string connectionString,
            string topicName,
            string subscriberName);


        protected abstract IMessageReceiver CreateMessageReceiver(string connectionString, string topicName, string subscriberName, ITokenProvider tokenProvider);
        protected MessageReceiverFactory(ILogger<MessageReceiverFactory> log, SubscriberConfiguration config)
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
                    _log.LogInformation($"Adding on subscription client {clientIndex} to list of source subscriptions");
                    if (_config.TokenProvider != null)
                    {

                        messageReceivers.Add(CreateMessageReceiver(connectionString,
                            _config.EffectiveTopicName, _config.SubscriberName, _config.TokenProvider));

                    }
                    else
                    {
                        messageReceivers.Add(CreateMessageReceiver(connectionString,
                            _config.EffectiveTopicName, _config.SubscriberName));
                    }
                }
                else
                {
                    _log.LogInformation($"Skipping subscription client {clientIndex}, connection string is null or empty");
                }
                clientIndex++;
            }
            return messageReceivers;
        }
    }
}
