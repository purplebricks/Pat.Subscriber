using System.Collections.Concurrent;
using log4net;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite
{
    public class SubscriptionClientBuilder
    {
        private readonly ILog _log;
        private readonly SubscriberConfiguration _config;

        public SubscriptionClientBuilder(ILog log, SubscriberConfiguration config)
        {
            _log = log;
            _config = config;
        }

        public ConcurrentQueue<SubscriptionClient> CreateClients(string subscriberName)
        {
            var clients = new ConcurrentQueue<SubscriptionClient>();

            var clientIndex = 1;
            foreach (var connectionString in _config.ConnectionStrings)
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _log.Info($"Adding on subscription client {clientIndex} to list of source subscriptions");
                    clients.Enqueue(BuildClient(connectionString, subscriberName));
                }
                else
                {
                    _log.Info($"Skipping subscription client {clientIndex}, connection string is null or empty");
                }
                clientIndex++;
            }

            return clients;
        }
      
        private SubscriptionClient BuildClient(string connectionString, string subscriberName)
        {
            return SubscriptionClient.CreateFromConnectionString(connectionString, _config.EffectiveTopicName, subscriberName);
        }
    }
}