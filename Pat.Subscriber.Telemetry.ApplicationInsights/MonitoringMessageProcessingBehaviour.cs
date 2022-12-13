using Microsoft.Azure.ServiceBus;
using Pat.Subscriber.MessageProcessing;

namespace Pat.Subscriber.Telemetry.ApplicationInsights
{
    public class MonitoringMessageProcessingBehaviour : IMessageProcessingBehaviour
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
            var messageType = GetMessageType(message);

            _statisticsReporter.Increment(_config.SubscriberName,
                messageType,
                result);
        }

        public async Task Invoke(Func<MessageContext, Task> next, MessageContext messageContext)
        {
            var messageType = GetMessageType(messageContext.Message);

            using var operation = _statisticsReporter.StartTimer(messageType);

            try
            {
                await next(messageContext).ConfigureAwait(false);
                ReportStats(messageContext.Message, "Success");
            }
            catch (Exception)
            {
                ReportStats(messageContext.Message, "Failed");
                throw;
            }
            finally
            {
                _statisticsReporter.StopTimer(operation);
            }
        }

        private string GetMessageType(Message message)
        {
            var messageType = message.UserProperties["MessageType"]?.ToString();

            if (string.IsNullOrWhiteSpace(messageType))
            {
                throw new ArgumentException("MessageType cannot be null");
            }

            return messageType;
        }
    }
}
