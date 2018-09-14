using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Pat.Subscriber.MessageMapping;
using Microsoft.Extensions.Logging;

namespace Pat.Subscriber
{
    public class Subscriber
    {
        private readonly ILogger _log;
        private readonly SubscriberConfiguration _config;
        private readonly MultipleBatchProcessor _multipleBatchProcessor;
        private readonly MessageReceiverFactory _messageReceiverFactory;
        private readonly SubscriptionBuilder _subscriptionBuilder;

        public Subscriber(ILogger<Subscriber> log,  SubscriberConfiguration config, MultipleBatchProcessor multipleBatchProcessor, MessageReceiverFactory messageReceiverFactory, SubscriptionBuilder subscriptionBuilder)
        {
            _log = log;
            _config = config;
            _multipleBatchProcessor = multipleBatchProcessor;
            _messageReceiverFactory = messageReceiverFactory;
            _subscriptionBuilder = subscriptionBuilder;
        }

        /// <summary>
        /// ReceiveMessages subscriptions and process messages.
        /// </summary>
        /// <param name="tokenSource"></param>
        /// <param name="handlerAssemblies">Assemblies containing handles, defaults to <code>Assembly.GetCallingAssembly()</code></param>
        public async Task Run(CancellationTokenSource tokenSource = null, Assembly[] handlerAssemblies = null)
        {
            if (await Initialise(handlerAssemblies).ConfigureAwait(false))
            {
                await ListenForMessages(tokenSource).ConfigureAwait(false);
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
            
            var messagesTypes = MessageMapper.GetHandledTypes().Select(t => t.FullName).ToArray();

            string handlerName = null;
            if (messagesTypes.Length == 0)
            {
                _log.LogWarning("Subscriber does not handle any message types");
            }
            else
            {
                var handler = MessageMapper.GetHandlerForMessageType(messagesTypes.First()).HandlerType;
                handlerName = handler.FullName;
            }

            _subscriptionBuilder.WithRuleVersionResolver(handlerAssemblies);
            return await _subscriptionBuilder.Build(messagesTypes, handlerName);
        }

        /// <summary>
        /// Process messages, terminate once the cancellation token is cancelled.
        /// </summary>
        public async Task ListenForMessages(CancellationTokenSource tokenSource = null)
        {
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

           var receivers = _messageReceiverFactory.CreateReceivers();

            _log.LogInformation("Listening for messages...");

            await _multipleBatchProcessor.ProcessMessages(receivers, tokenSource).ConfigureAwait(false);
        }
    }
}
