using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus.Messaging;
using PB.ITOps.Messaging.PatLite.MessageMapping;
using PB.ITOps.Messaging.PatLite.Policy;

namespace PB.ITOps.Messaging.PatLite
{
    public class Subscriber
    {
        private readonly ILog _log;
        private readonly IMessageProcessor _messageProcessor;
        private readonly ISubscriberPolicy _policy;
        private readonly SubscriberConfig _config;

        public Subscriber(ILog log, IMessageProcessor messageProcessor, ISubscriberPolicy policy, SubscriberConfig config)
        {
            _log = log;
            _messageProcessor = messageProcessor;
            _policy = policy;
            _config = config;
        }

        private void BootStrap()
        {
            MessageMapper.MapMessageTypesToHandlers();
            var builder = new SubscriptionBuilder(_log, _config);
            builder.Build(builder.SubscriptionRule(MessageMapper.GetHandledTypes().Select(t => t.FullName)),
                builder.CommonSubscriptionDescription());
        }
        private void ProcessMessages(ConcurrentQueue<BrokeredMessage> messages, ISubscriberPolicy policy)
        {
            Task.WaitAll(messages.Select(msg => _messageProcessor.ProcessMessage(msg, policy)).ToArray());
        }

        public void Run(CancellationTokenSource tokenSource = null)
        {
            BootStrap();

            var builder = new SubscriptionClientBuilder(_log, _config);
            var clients = builder.CreateClients(_config.SubscriberName);

            tokenSource = tokenSource ?? new CancellationTokenSource();
            while (!tokenSource.Token.IsCancellationRequested)
            {
                _policy.ProcessMessageBatch(() =>
                {
                    var messages = clients.GetMessages(_config.BatchSize);
                    if (messages.Any())
                    {
                        ProcessMessages(messages, _policy);
                    }
                }, tokenSource);
            }
        }
    }
}
