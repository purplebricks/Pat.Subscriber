using System.Threading.Tasks;

namespace Pat.Subscriber.RateLimiterPolicy
{
    public interface IThrottler
    {
        Task Delay(long milliseconds);
    }
}
