using System;

namespace PB.ITOps.Messaging.PatLite.MonitoringPolicy
{
    public interface IStatisticsReporter
    {
        IDisposable StartTimer(string @event, string tags);
        void Timer(string @event, string tags, int time);
        void Increment(string @event, string tags, int value = 1);
    }
}