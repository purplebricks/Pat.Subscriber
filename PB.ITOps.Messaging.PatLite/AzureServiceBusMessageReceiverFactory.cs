using log4net;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace PB.ITOps.Messaging.PatLite
{
    public class AzureServiceBusMessageReceiverFactory : MessageReceiverFactory
    {
        public AzureServiceBusMessageReceiverFactory(ILog log, SubscriberConfiguration config) : base(log, config)
        {
        }

        protected override IMessageReceiver CreateMessageReceiver(string connectionString, string topicName, string subscriberName)
        {
            return new MessageReceiver(connectionString,
                EntityNameHelper.FormatSubscriptionPath(topicName, subscriberName));
        }
    }
}