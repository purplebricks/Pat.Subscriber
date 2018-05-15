using System.Threading.Tasks;

namespace Pat.Subscriber
{
    public interface IHandleEvent<in T>
    {
        Task HandleAsync(T message);
    }
}
