using System;
using System.Runtime.Serialization;
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
            catch (SerializationException ex)
            {
                var messageType = GetMessageType(message);
                var correlationId = GetCollelationId(message);
                _log.Warn($"Unable to deserialise message body, message deadlettered. `{messageType}` correlation id `{correlationId}` on subscriber `{_config.SubscriberName}`.", ex);
                await message.DeadLetterAsync();
            }
            catch (Exception ex)
            {
                var messageType = GetMessageType(message);
                var correlationId = GetCollelationId(message);
                _log.Fatal($"Unhandled infrastructure exception on processing message type `{messageType}` correlation id `{correlationId}` on subscriber `{_config.SubscriberName}`.", ex);
                throw;
            }
        }

        private static string GetMessageType(BrokeredMessage message)
        {
            return message.Properties.ContainsKey("MessageType")
                ? message.Properties["MessageType"].ToString()
                : "Unknown Message Type";
        }

        private static string GetCollelationId(BrokeredMessage message)
        {
            return message.Properties.ContainsKey("PBCorrelationId")
                ? message.Properties["PBCorrelationId"].ToString()
                : "null";
        }

        protected override async Task<int> DoProcessMessageBatch(Func<Task<int>> action, CancellationTokenSource tokenSource)
        {
            try
            {
                return await action();
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

            return 0;
        }
    }
}
