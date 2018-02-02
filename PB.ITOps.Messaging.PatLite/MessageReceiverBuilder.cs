using System.Collections.Generic;
using log4net;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace PB.ITOps.Messaging.PatLite
{
    public class MessageReceiverBuilder
    {
        private readonly ILog _log;
        private readonly SubscriberConfiguration _config;

        public MessageReceiverBuilder(ILog log, SubscriberConfiguration config)
        {
            _log = log;
            _config = config;
        }

        public IList<IMessageReceiver> Build()
        {
            var messageReceivers = new List<IMessageReceiver>();

            var clientIndex = 1;
            foreach (var connectionString in _config.ConnectionStrings)
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _log.Info($"Adding on subscription client {clientIndex} to list of source subscriptions");
                    messageReceivers.Add(new MessageReceiver(connectionString, EntityNameHelper.FormatSubscriptionPath(_config.EffectiveTopicName, _config.SubscriberName)));
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