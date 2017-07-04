using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using PB.ITOps.Messaging.PatLite.Policy;
using StatsdClient;

namespace PB.ITOps.Messaging.PatLite.MonitoringPolicy
{
    public class MonitoringPolicy: BasePolicy
    {
        private readonly SubscriberConfig _config;
        private readonly MonitoringConfig _monitoringConfig;

        public MonitoringPolicy(SubscriberConfig config, MonitoringConfig monitoringConfig)
        {
            _config = config;
            _monitoringConfig = monitoringConfig;
            Metrics.Configure(new MetricsConfig
            {
                StatsdServerName = _monitoringConfig.StatsDHost,
                StatsdServerPort = _monitoringConfig.StatsDPort
            });
        }

        protected override async Task DoProcessMessage(Func<BrokeredMessage, Task> action, BrokeredMessage message)
        {
            using (Metrics.StartTimer("ProcessMessageTime," +
                                      $"Env=Client=PatLite.{_config.SubscriberName}," +
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

            Metrics.Counter("MessageProcessed," +
                            $"Client=PatLite.{_config.SubscriberName}," +
                            $"MessageType={messageType}," +
                            "CoreMessage=False," +
                            $"Result={result}," +
                            $"Bus={bus}," +
                            $"Env={_monitoringConfig.Environment}");

            Metrics.Timer("MessageProcessedFullTimeSec," +
                          $"Client=PatLite.{_config.SubscriberName}," +
                          $"MessageType={messageType}," +
                          "CoreMessage=False," +
                          $"Result={result}," +
                          $"Bus={bus}," +
                          $"Env={_monitoringConfig.Environment}",
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
