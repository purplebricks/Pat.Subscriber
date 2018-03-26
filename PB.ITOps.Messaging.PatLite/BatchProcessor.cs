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
        private readonly BatchFactory _batchFactory;
        private readonly ILog _log;

        public BatchProcessor(BatchProcessingBehaviourPipeline batchProcessingBehaviourPipeline,
            BatchFactory batchFactory,
            ILog log)
        {
            _batchProcessingBehaviourPipeline = batchProcessingBehaviourPipeline;
            _batchFactory = batchFactory;
            _log = log;
        }

        public Task ProcessBatch(IMessageReceiver messageReceiver, CancellationTokenSource tokenSource)
        {
            return _batchProcessingBehaviourPipeline.Invoke(async () =>
            {
                var batch = _batchFactory.Create();
                await batch.ReceiveMessages(messageReceiver).ConfigureAwait(false);
                if (batch.HasMessages)
                {
                    _log.Debug($"Message collection processing {batch.MessageCount} messages");
                    await batch.ProcessMessages().ConfigureAwait(false);
                }
            }, tokenSource);
        }
    }
}