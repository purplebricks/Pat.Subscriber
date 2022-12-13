using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Diagnostics;

namespace Pat.Subscriber.Telemetry.ApplicationInsights
{
    /// <inheritdoc />
    public class StatisticsReporter : IStatisticsReporter, IDisposable
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly Metric _messageCount;

        public StatisticsReporter(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
            _messageCount = _telemetryClient.GetMetric("MessageProcessed", "SubscriptionName", "MessageType", "Result");
        }

        public void Dispose()
        {
            if(_telemetryClient != null )
            {
                _telemetryClient.Flush();
            }
        }

        /// <inheritdoc />
        public IOperationHolder<RequestTelemetry> StartTimer(string messageType)
        {
            var requestActivity = new Activity("MessageProcessedFullTimeSec");
            requestActivity.AddTag("MessageType", messageType);
            
            return _telemetryClient.StartOperation<RequestTelemetry>(requestActivity);
        }

        public void StopTimer(IOperationHolder<RequestTelemetry> operation)
        {
            _telemetryClient.StopOperation(operation);
        }

        /// <inheritdoc />
        public void Increment(string subscriptionName, string messageType, string result, int value = 1)
        {
            _messageCount.TrackValue(value, subscriptionName, messageType, result);
        }
    }
}
