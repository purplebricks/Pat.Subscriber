using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Pat.Subscriber.BatchProcessing
{
    public class DefaultBatchProcessingBehaviour : IBatchProcessingBehaviour
    {
        private readonly ILogger _log;
        private readonly SubscriberConfiguration _config;

        public DefaultBatchProcessingBehaviour(ILogger log, SubscriberConfiguration config)
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
                        _log.LogWarning(ex, "MessagingCommunicationException was thrown, queue handler will retry to get messages from service bus soon.");
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        _log.LogError(ex, $"Unhandled non transient exception on queue {_config.SubscriberName}. Terminating queuehandler");
                        context.TokenSource.Cancel();
                    }
                    return true;
                });
            }
            catch (ServiceBusCommunicationException)
            {
                _log.LogWarning("MessagingCommunicationException was thrown, subscriber will retry to get messages from service bus soon.");
                Thread.Sleep(2000);
            }
            catch (Exception exception)
            {
                _log.LogError(exception, $"Unhandled non transient exception on queue {_config.SubscriberName}. Terminating queuehandler");
                context.TokenSource.Cancel();
            }
        }
    }
}
