using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;

namespace Pat.Subscriber
{
    public class AzureServiceBusMessageReceiverFactory : MessageReceiverFactory
    {
        public AzureServiceBusMessageReceiverFactory(ILogger<AzureServiceBusMessageReceiverFactory> log, SubscriberConfiguration config) : base(log, config)
        {
        }

        protected override IMessageReceiver CreateMessageReceiver(string connectionString, string topicName, string subscriberName)
        {
            return new MessageReceiver(connectionString,
                EntityNameHelper.FormatSubscriptionPath(topicName, subscriberName));
        }
    }
}