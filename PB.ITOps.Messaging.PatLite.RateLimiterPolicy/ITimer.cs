namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy
{
    public interface ITimer
    {
        void Start();
        long ElapsedMilliseconds { get; set; }
    }
}
