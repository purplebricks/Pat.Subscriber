using System;
using StatsdClient;

namespace Pat.Subscriber.Telemetry.StatsD
{
    public class StatisticsReporter : IStatisticsReporter
    {
        private const int DefaultPort = 8125;
        private readonly string _mandatoryTags;

        public StatisticsReporter(StatisticsReporterConfiguration setting)
        {
            _mandatoryTags = $"Env={setting.Environment},Tenant={setting.Tenant}";

            Metrics.Configure(new MetricsConfig
            {
                StatsdServerName = setting.StatsDHost,
                StatsdServerPort = setting.StatsDPort ?? DefaultPort
            });
        }

        public IDisposable StartTimer(string @event, string tags)
        {
            return Metrics.StartTimer($"{@event},{tags},{_mandatoryTags}");
        }

        public void Timer(string @event, string tags, int time)
        {
            Metrics.Timer($"{@event},{tags},{_mandatoryTags}", time);
        }

        public void Increment(string @event, string tags, int value = 1)
        {
            Metrics.Counter($"{@event},{tags},{_mandatoryTags}", value);
        }
    }
}