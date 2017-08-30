using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy
{
    public class StandardPolicy : BasePolicy
    {
        private readonly ILog _log;
        private readonly SubscriberConfiguration _config;

        public StandardPolicy(ILog log, SubscriberConfiguration config)
        {
            _log = log;
            _config = config;
        }
        protected override async Task DoProcessMessage(Func<BrokeredMessage, Task> action, BrokeredMessage message)
        {
            try
            {
                await action(message);
            }
            catch (Exception ex)
            {
                var messageType = message.Properties.ContainsKey("MessageType")
                    ? message.Properties["MessageType"]
                    : "Unknown Message Type";
                var correlationId = message.Properties.ContainsKey("PBCorrelationId")
                    ? message.Properties["PBCorrelationId"].ToString()
                    : "null";
                _log.Fatal($"Unhandled infrastructure exception on processing message type `{messageType}` correlation id `{correlationId}` on subscriber `{_config.SubscriberName}`.", ex);
                throw;
            }
        }

        protected override Task<int> DoProcessMessageBatch(Func<Task<int>> action, CancellationTokenSource tokenSource)
        {
            try
            {
                return action();
            }
            catch (AggregateException ae)
            {
                ae.Flatten().Handle(ex =>
                {
                    if (ex is MessagingCommunicationException)
                    {
                        _log.Warn("MessagingCommunicationException was thrown, queue handler will retry to get messages from service bus soon.", ex);
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        _log.Fatal($"Unhandled non transient exception on queue {_config.SubscriberName}. Terminating queuehandler", ex);
                        tokenSource.Cancel();
                    }
                    return true;
                });
            }
            catch (MessagingCommunicationException)
            {
                _log.Warn("MessagingCommunicationException was thrown, subscriber will retry to get messages from service bus soon.");
                Thread.Sleep(2000);
            }
            catch (Exception exception)
            {
                _log.Fatal($"Unhandled non transient exception on queue {_config.SubscriberName}. Terminating queuehandler", exception);
                tokenSource.Cancel();
            }

            return Task.FromResult(0);
        }
    }
}
