using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;
using System.Reflection;
using PB.ITOps.Messaging.PatLite.BatchProcessing;
using PB.ITOps.Messaging.PatLite.MessageMapping;
using PB.ITOps.Messaging.PatLite.SubscriberRules;

namespace PB.ITOps.Messaging.PatLite
{
    public class Subscriber
    {
        private readonly ILog _log;
        private readonly BatchProcessingBehaviourPipeline _batchProcessingBehaviourPipeline;
        private readonly SubscriberConfiguration _config;
        private readonly IMessageProcessor _messageProcessor;

        public Subscriber(ILog log, BatchProcessingBehaviourPipeline batchProcessingBehaviourPipeline, SubscriberConfiguration config, IMessageProcessor messageProcessor)
        {
            _log = log;
            _batchProcessingBehaviourPipeline = batchProcessingBehaviourPipeline;
            _config = config;
            _messageProcessor = messageProcessor;
        }

        /// <summary>
        /// Create subscriptions and process messages.
        /// </summary>
        /// <param name="tokenSource"></param>
        /// <param name="handlerAssemblies">Assemblies containing handles, defaults to <code>Assembly.GetCallingAssembly()</code></param>
        public async Task Run(CancellationTokenSource tokenSource = null, Assembly[] handlerAssemblies = null)
        {
            if (await Initialise(handlerAssemblies))
            {
                ListenForMessages(tokenSource);
            }
        }

        /// <summary>
        /// Creates relevant subscriptions.
        /// </summary>
        /// <param name="handlerAssemblies">Assemblies containing handles, defaults to <code>Assembly.GetCallingAssembly()</code></param>
        public async Task<bool> Initialise(Assembly[] handlerAssemblies)
        {
            if (handlerAssemblies == null || handlerAssemblies.Length == 0)
            {
                throw new ArgumentException("One or more assemblies required", nameof(handlerAssemblies));
            }

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

            return await builder.Build(messagesTypes, handlerName);
        }

        /// <summary>
        /// Process messages, terminate once the cancellation token is cancelled.
        /// </summary>
        public void ListenForMessages(CancellationTokenSource tokenSource = null)
        {
            var builder = new MessageReceiverBuilder(_log, _config);
            var clients = builder.Build();

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

        private async Task ProcessBatchChain(IList<IMessageReceiver> messageReceivers, CancellationTokenSource tokenSource, int batchIndex)
        {
            var processor = new BatchProcessor(_batchProcessingBehaviourPipeline, _messageProcessor, _log, batchIndex);
            var tasks = messageReceivers.Select(messageReceiver => Task.Run(async () =>
            {
                while (!tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await processor.ProcessBatch(
                            messageReceiver,
                            tokenSource,
                            _config.BatchSize,
                            _config.ReceiveTimeout);
                    }
                    catch (Exception exception)
                    {
                        _log.Fatal($"Unhandled non transient exception on queue {_config.SubscriberName}. Terminating queuehandler from ProcessBatchChain.", exception);
                        tokenSource.Cancel();
                    }
                }
            }));

            await Task.WhenAll(tasks.ToArray());
        }
    }
}
