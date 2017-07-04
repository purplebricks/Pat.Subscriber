using System;
using System.Threading;
using log4net;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.Policy
{
    public class StandardPolicy : BasePolicy
    {
        private readonly ILog _log;
        private readonly SubscriberConfig _config;

        public StandardPolicy(ILog log, SubscriberConfig config)
        {
            _log = log;
            _config = config;
        }
        protected override void DoProcessMessageBatch(Action action, CancellationTokenSource tokenSource)
        {
            try
            {
                action();
            }
            catch (AggregateException ae)
            {
                ae.Flatten().Handle(ex =>
                {
                    if (ex is MessagingCommunicationException)
                    {
                        _log.Warn(
                            "MessagingCommunicationException was thrown, queue handler will retry to get messages from service bus soon.");
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        _log.Fatal(
                            $"Unhandled non transient exception on queue {_config.SubscriberName}. Terminating queuehandler",
                            ex);
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
        }

        public override void OnComplete(BrokeredMessage message)
        {
            base.OnComplete(message);
            _log.Info($"{_config.SubscriberName} Success Handling Message {message.SequenceNumber}: {message.ContentType}");
            message.Complete();
        }

        public override void OnFailure(BrokeredMessage message, Exception ex)
        {
            base.OnFailure(message, ex);
            _log.Info($"Message {message.SequenceNumber} failed", ex);
        }
    }

}
