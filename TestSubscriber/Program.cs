using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus.Messaging;
using PB.ITOps.Messaging.PatLite;
using PB.ITOps.Messaging.PatLite.MessageMapping;
using PB.ITOps.Messaging.PatSender;
using StructureMap;
using TestSubscriber.IoC;

namespace TestSubscriber
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
    
    class Program
    { 
        private static string BootStrap(ILog log, SubscriberConfig subscriberConfig)
        {
            var builder = new SubscriptionBuilder(log, subscriberConfig);
            string subscriberName = GetHandlerName();
            IEnumerable<string> messagesWeHandle = GetMessagesWeHandle();
            builder.Build(subscriberName,
                subscriberConfig.UsePartitioning,
                builder.CommonSubscriptionRule(subscriberName, messagesWeHandle, 1),
                builder.CommonSubscriptionDescription(subscriberName),
                1);
            RegisterHandlers();
            return subscriberName;
        }

        private static void RegisterHandlers()
        {
            throw new NotImplementedException();
        }

        private static IEnumerable<string> GetMessagesWeHandle()
        {
            throw new NotImplementedException();
        }

        private static string GetHandlerName()
        {
            return "Rightmove";
        }

        private static void ProcessMessages(ConcurrentQueue<BrokeredMessage> messages, IContainer container)
        {
            var processor = new MessageProcessor(new StructureMapDependencyResolver(container));
            Task.WaitAll(messages.Select(msg => processor.ProcessMessage(msg)).ToArray());
        }


        static void Main(string[] args)
        {
            MessageMapper.MapMessageTypesToHandlers();
            var connection = "Endpoint=sb://***REMOVED***.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=***REMOVED***";
            var topicName = "pat2G5FKC2";
            var subscriberConfig = new SubscriberConfig
            {
                ConnectionStrings = new[] { connection },
                TopicName = topicName,
                UsePartitioning = true
            };

            var container = IoC.IoC.Initialize();
            var log = container.GetInstance<ILog>();

            var publisher = new PB.ITOps.Messaging.PatSender.MessagePublisher(
                new PB.ITOps.Messaging.PatSender.MessageSender(
                    log,
                    new PatSenderSettings
                    {
                        PrimaryConnection = connection,
                        TopicName = topicName
                    }
                ));

            publisher.Publish(new MyDerivedEvent2()).Wait();

            //var subscriberName = BootStrap(log, subscriberConfig);

            var builder = new SubscriptionClientBuilder(log, subscriberConfig);
            var clients = builder.CreateClients("Rightmove");
            var messages = clients.GetMessages(1);
            if (messages.Any())
            {
                ProcessMessages(messages, container);
            }
        }
    }
}
