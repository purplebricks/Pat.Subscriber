using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Pat.Subscriber.MessageProcessing;

namespace Pat.Subscriber.Telemetry.StatsD
{
    public class MonitoringMessageProcessingBehaviour: IMessageProcessingBehaviour
    {
        private readonly SubscriberConfiguration _config;
        private readonly IStatisticsReporter _statisticsReporter;

        public MonitoringMessageProcessingBehaviour(SubscriberConfiguration config, IStatisticsReporter statisticsReporter)
        {
            _config = config;
            _statisticsReporter = statisticsReporter;
        }

        private void ReportStats(Message message, string result)
        {
            var fullTime = DateTime.UtcNow - message.ScheduledEnqueueTimeUtc;
            var messageType = message.UserProperties["MessageType"];
            var bus = message.RetrieveServiceBusAddressWithOnlyLetters();

            _statisticsReporter.Increment("MessageProcessed", 
                $"Client=PatLite.{_config.SubscriberName}," +
                $"MessageType={messageType}," +
                "CoreMessage=False," +
                $"Result={result}," +
                $"Bus={bus}");

            _statisticsReporter.Timer($"MessageProcessedFullTimeSec", 
                $"Client=PatLite.{_config.SubscriberName}," +
                $"MessageType={messageType}," +
                "CoreMessage=False," +
                $"Result={result}," +
                $"Bus={bus},",
                fullTime);
        }

        public async Task Invoke(Func<MessageContext, Task> next, MessageContext messageContext)
        {
            using (_statisticsReporter.StartTimer("ProcessMessageTime",
                $"Client=PatLite.{_config.SubscriberName}," +
                $"MessageType={messageContext.Message.UserProperties["MessageType"]}," +
                "CoreMessage=FALSE"))
            {
                try
                {
                    await next(messageContext).ConfigureAwait(false);
                    ReportStats(messageContext.Message, "Success");
                }
                catch (SerializationException ex)
                {
                    _statisticsReporter.Increment("ProcessMessageInfrastructureException",
                        $"Client=PatLite.{_config.SubscriberName}," +
                        $"MessageType={messageContext.Message.UserProperties["MessageType"]}," +
                        "CoreMessage=FALSE" +
                        $"ExceptionType={ex.GetType()}");
                    ReportStats(messageContext.Message, "Failed");
                    throw;
                }
                catch (Exception)
                {
                    ReportStats(messageContext.Message, "Failed");
                    throw;
                }
            }
        }
    }
}
