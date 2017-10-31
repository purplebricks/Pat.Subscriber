using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy;
using Purplebricks.StatsD.Client;

namespace PB.ITOps.Messaging.PatLite.MonitoringPolicy
{
    public class MonitoringPolicy: BasePolicy
    {
        private readonly SubscriberConfiguration _config;

        public MonitoringPolicy(SubscriberConfiguration config)
        {
            _config = config;
        }

        protected override async Task DoProcessMessage(Func<BrokeredMessage, Task> action, BrokeredMessage message)
        {
            using (StatsDSender.StartTimer("ProcessMessageTime", 
                $"Client=PatLite.{_config.SubscriberName}," +
                $"MessageType={message.Properties["MessageType"]}," +
                "CoreMessage=FALSE"))
            {
                try
                {
                    await action(message);
                }
                catch (Exception ex)
                {
                    StatsDSender.Increment("ProcessMessageInfrastructureException", 
                        $"Client=PatLite.{_config.SubscriberName}," +
                        $"MessageType={message.Properties["MessageType"]}," +
                        "CoreMessage=FALSE" + 
                        $"ExceptionType={ex.GetType()}");
                    throw;
                }
            }
        }

        protected override async Task<int> DoProcessMessageBatch(Func<Task<int>> action, CancellationTokenSource tokenSource)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                StatsDSender.Increment("ProcessBatchInfrastructureException",
                    $"Client=PatLite.{_config.SubscriberName}," +
                    $"ExceptionType={ex.GetType()}");
                throw;
            }
        }

        private void SendResultToStatsD(BrokeredMessage message, string result)
        {
            var fullTime = (int)(DateTime.UtcNow - message.EnqueuedTimeUtc).TotalSeconds;
            var messageType = message.Properties["MessageType"];
            var bus = message.RetrieveServiceBusAddressWithOnlyLetters();

            StatsDSender.Increment("MessageProcessed", 
                $"Client=PatLite.{_config.SubscriberName}," +
                $"MessageType={messageType}," +
                "CoreMessage=False," +
                $"Result={result}," +
                $"Bus={bus}");

            StatsDSender.Timer($"MessageProcessedFullTimeSec", 
                $"Client=PatLite.{_config.SubscriberName}," +
                $"MessageType={messageType}," +
                "CoreMessage=False," +
                $"Result={result}," +
                $"Bus={bus},",
                Math.Max(0, fullTime));
        }

        protected override Task<bool> MessageHandlerCompleted(BrokeredMessage message, string body)
        {
            SendResultToStatsD(message, "Success");
            return Task.FromResult(true);
        }

        protected override Task<bool> MessageHandlerFailed(BrokeredMessage message, string body, Exception ex)
        {
            SendResultToStatsD(message, "Failed");
            return Task.FromResult(true);
        }
    }
}
