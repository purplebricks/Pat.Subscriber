using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using PB.ITOps.Messaging.PatLite.Policy;
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
                await action(message);
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
                fullTime);
        }

        public override void OnComplete(BrokeredMessage message)
        {
            base.OnComplete(message);
            SendResultToStatsD(message, "Success");
        }

        public override void OnFailure(BrokeredMessage message, Exception ex)
        {
            base.OnFailure(message, ex);
            SendResultToStatsD(message, "Failed");
        }
    }
}
