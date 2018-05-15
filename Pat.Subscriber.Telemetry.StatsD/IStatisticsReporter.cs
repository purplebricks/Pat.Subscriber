using System;

namespace Pat.Subscriber.Telemetry.StatsD
{
    public interface IStatisticsReporter
    {
        IDisposable StartTimer(string @event, string tags);
        void Timer(string @event, string tags, int time);
        void Increment(string @event, string tags, int value = 1);
    }
}