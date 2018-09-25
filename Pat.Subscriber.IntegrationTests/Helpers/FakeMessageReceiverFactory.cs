using log4net;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;

namespace Pat.Subscriber.IntegrationTests.Helpers
{
    public class FakeMessageReceiverFactory: MessageReceiverFactory
    {
        private readonly IMessageReceiver _messageReceiver;

        public FakeMessageReceiverFactory(ILogger<FakeMessageReceiverFactory> log, SubscriberConfiguration config, IMessageReceiver fakeMessageReceiver) : base(log, config)
        {
            _messageReceiver = fakeMessageReceiver;
        }

        protected override IMessageReceiver CreateMessageReceiver(string connectionString, string topicName, string subscriberName)
        {
            return _messageReceiver;
        }
    }
}
