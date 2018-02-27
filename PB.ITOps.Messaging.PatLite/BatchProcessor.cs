using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Azure.ServiceBus;
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

        public Task ProcessBatch(IMessageReceiver messageReceiver, CancellationTokenSource tokenSource, int batchSize, int receiveTimeout)
        {
            return _batchProcessingBehaviourPipeline.Invoke(async ()  => 
            {
                var messages = await messageReceiver.GetMessages(batchSize, receiveTimeout);
                if (messages.Any())
                {
                    _log.Debug($"Batch index {_batchIndex} processing {messages.Count} messages");
                    await ProcessMessages(messages, messageReceiver);
                }
            }, tokenSource);
        }

        private async Task ProcessMessages(ICollection<Message> messages, IMessageReceiver messageReceiver)
        {
            await Task.WhenAll(messages.Select(m => _messageProcessor.ProcessMessage(m, messageReceiver)).ToArray());
        }
    }
}