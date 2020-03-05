using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Primitives;
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
        
        protected override IMessageReceiver CreateMessageReceiver(string connectionString, string topicName, string subscriberName, ITokenProvider tokenProvider)
        {
            ServiceBusConnectionStringBuilder builder = new ServiceBusConnectionStringBuilder(connectionString);

            return new MessageReceiver(builder.Endpoint,
                EntityNameHelper.FormatSubscriptionPath(topicName, subscriberName),tokenProvider);
        }
    }
}
