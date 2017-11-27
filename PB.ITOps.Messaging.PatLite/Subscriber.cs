using System;
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
        private readonly ISubscriberPolicy _policy;
        private readonly SubscriberConfiguration _config;
        private readonly IMessageProcessor _messageProcessor;

        public event EventHandler SubscriptionSetupCompleted;

        public Subscriber(ILog log, ISubscriberPolicy policy, SubscriberConfiguration config, IMessageProcessor messageProcessor)
        {
            _log = log;
            _policy = policy;
            _config = config;
            _messageProcessor = messageProcessor;
        }

        private void BootStrap(Assembly[] handlerAssemblies)
        {
            MessageMapper.MapMessageTypesToHandlers(handlerAssemblies);
            var builder = new SubscriptionBuilder(_log, _config, new RuleVersionResolver(handlerAssemblies));
            var messagesTypes = MessageMapper.GetHandledTypes().Select(t => t.FullName).ToArray();

            string handlerName = null;
            if (messagesTypes.Length == 0)
            {
                _log.Warn("Subscriber does not handle any message types");
            }
            else
            {
                var handler = MessageMapper.GetHandlerForMessageType(messagesTypes.First()).HandlerType;
                handlerName = handler.FullName;
            }
            builder.Build(builder.CommonSubscriptionDescription(), messagesTypes, handlerName);

            SubscriptionSetupCompleted?.Invoke(this, new EventArgs());
        }

        public void Run(CancellationTokenSource tokenSource = null, Assembly[] handlerAssemblies = null)
        {
            if (handlerAssemblies == null)
            {
                handlerAssemblies = new [] { Assembly.GetCallingAssembly() };
            }

            BootStrap(handlerAssemblies);

            var builder = new SubscriptionClientBuilder(_log, _config);
            var clients = builder.CreateClients(_config.SubscriberName);

            _log.Info("Listening for messages...");

            tokenSource = tokenSource ?? new CancellationTokenSource();
            if (_config.ConcurrentBatches == 0)
            {
                _config.ConcurrentBatches = 1;
            }

            if (_config.ConcurrentBatches < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(_config.ConcurrentBatches), 
                    $"Cannot support {_config.ConcurrentBatches} concurrent batches.");
            }

            var tasks = Enumerable.Range(0, _config.ConcurrentBatches)
                .Select(_ => 
                    Task.Run(async () => await ProcessBatchChain(clients, tokenSource, _), tokenSource.Token));

            Task.WaitAll(tasks.ToArray());
        }

        private async Task ProcessBatchChain(ConcurrentQueue<SubscriptionClient> clients, CancellationTokenSource tokenSource, int batchIndex)
        {
            var processor = new BatchProcessor(_policy, _messageProcessor, _log, batchIndex);
            while (!tokenSource.IsCancellationRequested)
            {
                try
                {
                    await processor.ProcessBatch(clients, tokenSource, _config.BatchSize);
                }
                catch (Exception exception)
                {
                    _log.Fatal($"Unhandled non transient exception on queue {_config.SubscriberName}. Terminating queuehandler from ProcessBatchChain.", exception);
                    tokenSource.Cancel();
                }
            }
        }
    }
}
