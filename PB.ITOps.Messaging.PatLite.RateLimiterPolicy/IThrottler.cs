using System.Threading.Tasks;

namespace PB.ITOps.Messaging.PatLite.RateLimiterPolicy
{
    public interface IThrottler
    {
        Task Delay(long milliseconds);
    }
}
