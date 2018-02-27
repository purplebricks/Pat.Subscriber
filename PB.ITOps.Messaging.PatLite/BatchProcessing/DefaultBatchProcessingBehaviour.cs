using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Azure.ServiceBus;

namespace PB.ITOps.Messaging.PatLite.BatchProcessing
{
    public class DefaultBatchProcessingBehaviour : IBatchProcessingBehaviour
    {
        private readonly ILog _log;
        private readonly SubscriberConfiguration _config;

        public DefaultBatchProcessingBehaviour(ILog log, SubscriberConfiguration config)
        {
            _log = log;
            _config = config;
        }

        public async Task Invoke(Func<BatchContext, Task> next, BatchContext context)
        {
            try
            {
                await context.Action();
            }
            catch (AggregateException ae)
            {
                ae.Flatten().Handle(ex =>
                {
                    if (ex is ServiceBusCommunicationException)
                    {
                        _log.Warn("MessagingCommunicationException was thrown, queue handler will retry to get messages from service bus soon.", ex);
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        _log.Fatal($"Unhandled non transient exception on queue {_config.SubscriberName}. Terminating queuehandler", ex);
                        context.TokenSource.Cancel();
                    }
                    return true;
                });
            }
            catch (ServiceBusCommunicationException)
            {
                _log.Warn("MessagingCommunicationException was thrown, subscriber will retry to get messages from service bus soon.");
                Thread.Sleep(2000);
            }
            catch (Exception exception)
            {
                _log.Fatal($"Unhandled non transient exception on queue {_config.SubscriberName}. Terminating queuehandler", exception);
                context.TokenSource.Cancel();
            }
        }
    }
}
