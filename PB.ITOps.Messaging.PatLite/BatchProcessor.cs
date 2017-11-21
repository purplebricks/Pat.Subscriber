using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus.Messaging;
using PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy;

namespace PB.ITOps.Messaging.PatLite
{
    public class BatchProcessor
    {
        private readonly ISubscriberPolicy _policy;
        private readonly IMessageProcessor _messageProcessor;
        private readonly ILog _log;
        private readonly int _batchIndex;

        public BatchProcessor(ISubscriberPolicy policy, IMessageProcessor messageProcessor, ILog log, int batchIndex)
        {
            _policy = policy;
            _messageProcessor = messageProcessor;
            _log = log;
            _batchIndex = batchIndex;
        }

        public Task ProcessBatch(ConcurrentQueue<SubscriptionClient> clients, CancellationTokenSource tokenSource, int batchSize)
        {
            return _policy.ProcessMessageBatch(() =>
            {
                var messages = clients.GetMessages(batchSize);
                if (messages.Any())
                {
                    _log.Debug($"Batch index {_batchIndex} processing {messages.Count} messages");
                    return ProcessMessages(messages);
                }
                return Task.FromResult(0);
            }, tokenSource);
        }

        private async Task<int> ProcessMessages(IReadOnlyCollection<BrokeredMessage> messages)
        {
            await Task.WhenAll(messages.Select(ProcessMessage).ToArray());
            return messages.Count;
        }

        private async Task ProcessMessage(BrokeredMessage message)
        {
            using (message)
            {
                await _policy.ProcessMessage(m => _messageProcessor.ProcessMessage(m, _policy), message);
            }
        }
    }
}