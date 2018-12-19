using System;
using JustEat.StatsD;

namespace Pat.Subscriber.Telemetry.StatsD
{
    /// <inheritdoc />
    public class StatisticsReporter : IStatisticsReporter
    {
        private const int DefaultPort = 8125;
        private const string DefaultHost = "localhost";
        private readonly string _mandatoryTags;
        private readonly IStatsDPublisher _statsDPublisher;

        public StatisticsReporter(StatisticsReporterConfiguration setting)
        {
            _mandatoryTags = $"Env={setting.Environment},Tenant={setting.Tenant}";

            _statsDPublisher = new StatsDPublisher(new StatsDConfiguration 
            {
                Host = string.IsNullOrEmpty(setting.StatsDHost) ? DefaultHost : setting.StatsDHost,
                Port = setting.StatsDPort ?? DefaultPort
            });
        }

        /// <inheritdoc />
        public IDisposable StartTimer(string @event, string tags)
        {
            return _statsDPublisher.StartTimer($"{@event},{tags},{_mandatoryTags}");
        }

        /// <inheritdoc />
        public void Timer(string @event, string tags, TimeSpan duration)
        {
            _statsDPublisher.Timing(duration, $"{@event},{tags},{_mandatoryTags}");
        }

        /// <inheritdoc />
        public void Increment(string @event, string tags, int value = 1)
        {
            _statsDPublisher.Increment(value, $"{@event},{tags},{_mandatoryTags}");
        }
    }
}