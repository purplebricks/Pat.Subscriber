using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Pat.Subscriber.Telemetry.ApplicationInsights
{
    /// <summary>
    /// Defines an interface for publishing statistics to StatsD.
    /// </summary>
    public interface IStatisticsReporter
    {
        /// <summary>
        /// Start a telemetry operation
        /// </summary>
        /// <param name="event">The name of the event which is being timed.</param>
        /// <param name="tags">Related tags.</param>
        /// <returns></returns>
        IOperationHolder<RequestTelemetry> StartTimer(string messageType);

        /// <summary>
        /// Mark the operation as finished
        /// </summary>
        /// <param name="operation"></param>
        void StopTimer(IOperationHolder<RequestTelemetry> operation);

        /// <summary>
        /// Publishes a counter for the specified bucket and value.
        /// </summary>
        /// <param name="subscriptionName">The name of the subscription</param>
        /// <param name="bus">The namespace of the service bus</param>
        /// <param name="messageType">The name of the c# type that wraps the messages</param>
        /// <param name="result">Successful or Failed</param>
        /// <param name="value">Number of messages processed</param>
        void Increment(string subscriptionName, string messageType, string result, int value = 1);
    }
}
