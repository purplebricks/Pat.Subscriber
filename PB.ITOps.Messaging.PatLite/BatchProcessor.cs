using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Azure.ServiceBus.Core;
using PB.ITOps.Messaging.PatLite.BatchProcessing;

namespace PB.ITOps.Messaging.PatLite
{
    public class BatchProcessor
    {
        private readonly BatchProcessingBehaviourPipeline _batchProcessingBehaviourPipeline;
        private readonly IMessageProcessor _messageProcessor;
        private readonly ILog _log;
        private readonly int _batchIndex;

        public BatchProcessor(BatchProcessingBehaviourPipeline batchProcessingBehaviourPipeline, IMessageProcessor messageProcessor, ILog log, int batchIndex)
        {
            _batchProcessingBehaviourPipeline = batchProcessingBehaviourPipeline;
            _messageProcessor = messageProcessor;
            _log = log;
            _batchIndex = batchIndex;
        }

        public Task ProcessBatch(IList<IMessageReceiver> messageReceivers, CancellationTokenSource tokenSource, int batchSize)
        {
            return _batchProcessingBehaviourPipeline.Invoke(() =>
            {
                var messages = messageReceivers.GetMessages(batchSize);
                if (messages.Any())
                {
                    _log.Debug($"Batch index {_batchIndex} processing {messages.Count} messages");
                    return ProcessMessages(messages);
                }
                return Task.FromResult(0);
            }, tokenSource);
        }

        private async Task<int> ProcessMessages(ICollection<MessageClientPair> messages)
        {
            await Task.WhenAll(messages.Select(m => _messageProcessor.ProcessMessage(m.Message, m.MessageReceiver)).ToArray());
            return messages.Count;
        }
    }
}