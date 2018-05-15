namespace Pat.Subscriber.RateLimiterPolicy
{
    public interface ITimer
    {
        void Start();
        long ElapsedMilliseconds { get; set; }
    }
}
