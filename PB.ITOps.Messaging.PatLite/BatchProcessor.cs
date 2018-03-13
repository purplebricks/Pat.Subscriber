using System;
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
        private readonly BatchConfiguration _config;
        private readonly ILog _log;

        public BatchProcessor(BatchProcessingBehaviourPipeline batchProcessingBehaviourPipeline,
            IMessageProcessor messageProcessor,
            BatchConfiguration config,
            ILog log)
        {
            _batchProcessingBehaviourPipeline = batchProcessingBehaviourPipeline;
            _messageProcessor = messageProcessor;
            _config = config;
            _log = log;
        }

        public Task ProcessBatch(IMessageReceiver messageReceiver, CancellationTokenSource tokenSource)
        {
            return _batchProcessingBehaviourPipeline.Invoke(async () =>
            {
                var receivedMessages = await MessageCollection.ReceiveMessages(messageReceiver, _config.BatchSize, _config.ReceiveTimeoutSeconds).ConfigureAwait(false);
                if (receivedMessages.Any)
                {
                    _log.Debug($"Message collection processing {receivedMessages.Count} messages");
                    await receivedMessages.Process(_messageProcessor).ConfigureAwait(false);
                }
            }, tokenSource);
        }
    }
}