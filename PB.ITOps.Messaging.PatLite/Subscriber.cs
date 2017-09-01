using log4net;
using Microsoft.ServiceBus.Messaging;
using PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy;
using PB.ITOps.Messaging.PatLite.MessageMapping;
using PB.ITOps.Messaging.PatLite.SubscriberRules;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PB.ITOps.Messaging.PatLite
{
    public class Subscriber
    {
        private readonly ILog _log;
        private readonly IMessageProcessor _messageProcessor;
        private readonly ISubscriberPolicy _policy;
        private readonly SubscriberConfiguration _config;

        public Subscriber(ILog log, IMessageProcessor messageProcessor, ISubscriberPolicy policy, SubscriberConfiguration config)
        {
            _log = log;
            _messageProcessor = messageProcessor;
            _policy = policy;
            _config = config;
        }

        private void BootStrap(Assembly[] handlerAssemblies)
        {
            MessageMapper.MapMessageTypesToHandlers(handlerAssemblies);
            var builder = new SubscriptionBuilder(_log, _config, new RuleVersionResolver(handlerAssemblies));
            var messagesTypes = MessageMapper.GetHandledTypes().Select(t => t.FullName).ToArray();
            builder.Build(builder.CommonSubscriptionDescription(), messagesTypes);
        }
        private async Task<int> ProcessMessages(ConcurrentQueue<BrokeredMessage> messages, ISubscriberPolicy policy)
        {
            await Task.WhenAll(messages.Select(msg => ProcessMessage(msg, policy)).ToArray());
            return messages.Count;
        }

        private async Task ProcessMessage(BrokeredMessage message, ISubscriberPolicy policy)
        {
            using (message)
            {
                await policy.ProcessMessage(m => _messageProcessor.ProcessMessage(m, policy), message);
            }
        }

        public void Run(CancellationTokenSource tokenSource = null, Assembly[] handlerAssemblies = null)
        {
            if (handlerAssemblies == null)
            {
                handlerAssemblies = new Assembly[] { Assembly.GetCallingAssembly() };
            }

            BootStrap(handlerAssemblies);

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
                        return ProcessMessages(messages, _policy);
                    }
                    return Task.FromResult(0);
                }, tokenSource).Wait();
            }
        }
    }
}
