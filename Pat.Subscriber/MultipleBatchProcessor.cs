using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Azure.ServiceBus.Core;

namespace Pat.Subscriber
{
    public class MultipleBatchProcessor
    {
        private readonly BatchProcessor _batchProcessor;
        private readonly string _subscriberName;
        private readonly ILog _log;

        public MultipleBatchProcessor(BatchProcessor batchProcessor, ILog log, string subscriberName)
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
                                _log.Fatal(
                                    $"Unhandled non transient exception on queue {_subscriberName}. Terminating queuehandler from ProcessBatchChain.",
                                    exception);
                                tokenSource.Cancel();
                            }
                        }
                        _log.Info("Shutting down...");
                    }, tokenSource.Token));

            return Task.WhenAll(tasks.ToArray());
        }
    }
}