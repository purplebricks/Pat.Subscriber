using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus.Messaging;
using PB.ITOps.Messaging.PatLite.MessageMapping;

namespace PB.ITOps.Messaging.PatLite
{
    public static class Helpers
    {
        private const string AddressKey = "SubscriptionClientAddress";

        public static ConcurrentQueue<BrokeredMessage> GetMessages(this ConcurrentQueue<SubscriptionClient> clients, int batchSize)
        {
            var messageQueue = new ConcurrentQueue<BrokeredMessage>();
            Task.WaitAll(clients.Select(c => c.QueueMessages(messageQueue, batchSize)).ToArray());
            return messageQueue;
        }

        private static async Task QueueMessages(this SubscriptionClient c, ConcurrentQueue<BrokeredMessage> queueMessages, int batchSize)
        {
            var messages = await c.ReceiveBatchAsync(batchSize, TimeSpan.FromSeconds(1));
            foreach (var message in messages)
            {
                message.Properties.Add(AddressKey, c.MessagingFactory.Address.ToString());
                queueMessages.Enqueue(message);
            }
        }
    }

    public class Subscriber
    {
        private readonly ILog _log;
        private readonly IMessageProcessor _messageProcessor;
        private readonly SubscriberConfig _config;

        public Subscriber(ILog log, IMessageProcessor messageProcessor, SubscriberConfig config)
        {
            _log = log;
            _messageProcessor = messageProcessor;
            _config = config;
        }

        private void BootStrap()
        {
            MessageMapper.MapMessageTypesToHandlers();
            var builder = new SubscriptionBuilder(_log, _config);
            builder.Build(builder.SubscriptionRule(MessageMapper.GetHandledTypes().Select(t => t.FullName)),
                builder.CommonSubscriptionDescription());
        }
        private void ProcessMessages(ConcurrentQueue<BrokeredMessage> messages)
        {
            Task.WaitAll(messages.Select(msg => _messageProcessor.ProcessMessage(msg)).ToArray());
        }
        public void Run()
        {
            BootStrap();

            var builder = new SubscriptionClientBuilder(_log, _config);
            var clients = builder.CreateClients(_config.SubscriberName);
            var messages = clients.GetMessages(_config.BatchSize);
            if (messages.Any())
            {
                ProcessMessages(messages);
            }
        }
    }
}
