using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using Pat.Subscriber.BatchProcessing;

namespace Pat.Subscriber
{
    public class BatchProcessor
    {
        private readonly BatchProcessingBehaviourPipeline _batchProcessingBehaviourPipeline;
        private readonly BatchFactory _batchFactory;
        private readonly ILogger _log;

        public BatchProcessor(BatchProcessingBehaviourPipeline batchProcessingBehaviourPipeline,
            BatchFactory batchFactory,
            ILogger log)
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
                    _log.LogDebug($"Message collection processing {batch.MessageCount} messages");
                    await batch.ProcessMessages().ConfigureAwait(false);
                }
            }, tokenSource);
        }
    }
}