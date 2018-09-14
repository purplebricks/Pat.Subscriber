using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;

namespace Pat.Subscriber
{
    public class MultipleBatchProcessor
    {
        private readonly BatchProcessor _batchProcessor;
        private readonly string _subscriberName;
        private readonly ILogger _log;

        public MultipleBatchProcessor(BatchProcessor batchProcessor, ILogger<MultipleBatchProcessor> log, string subscriberName)
        {
            _batchProcessor = batchProcessor;
            _log = log;
            _subscriberName = subscriberName;
        }

        public virtual Task ProcessMessages(IList<IMessageReceiver> messageReceivers,
            CancellationTokenSource tokenSource)
        {
            var tasks = messageReceivers
                .Select(messageReceiver =>
                    Task.Run(async () =>
                    {
                        while (!tokenSource.IsCancellationRequested)
                        {
                            try
                            {
                                await _batchProcessor.ProcessBatch(messageReceiver, tokenSource).ConfigureAwait(false);
                            }
                            catch (Exception exception)
                            {
                                _log.LogError(
                                    exception,
                                    $"Unhandled non transient exception on queue {_subscriberName}. Terminating queuehandler from ProcessBatchChain.");
                                tokenSource.Cancel();
                            }
                        }
                        _log.LogInformation("Shutting down...");
                    }, tokenSource.Token));

            return Task.WhenAll(tasks.ToArray());
        }
    }
}