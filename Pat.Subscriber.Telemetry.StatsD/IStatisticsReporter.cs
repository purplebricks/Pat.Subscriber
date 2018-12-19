using System;

namespace Pat.Subscriber.Telemetry.StatsD
{
    /// <summary>
    /// Defines an interface for publishing statistics to StatsD.
    /// </summary>
    public interface IStatisticsReporter
    {
        /// <summary>
        /// Start a timer for use in a `using` statement.
        /// </summary>
        /// <param name="event">The name of the event which is being timed.</param>
        /// <param name="tags">Related tags.</param>
        /// <returns></returns>
        IDisposable StartTimer(string @event, string tags);

        /// <summary>
        /// Publishes a timer for the specified bucket and value.
        /// </summary>
        /// <param name="event">The name of the event which is being timed.</param>
        /// <param name="tags">Related tags.</param>
        /// <param name="duration">The value to publish for the timer.</param>
        void Timer(string @event, string tags, TimeSpan duration);
        
        /// <summary>
        /// Publishes a counter for the specified bucket and value.
        /// </summary>
        /// <param name="event">The name of the counter which is being incremented.</param>
        /// <param name="tags">Related tags.</param>
        /// <param name="value">The value to increment the counter by.</param>
        void Increment(string @event, string tags, int value = 1);
    }
}