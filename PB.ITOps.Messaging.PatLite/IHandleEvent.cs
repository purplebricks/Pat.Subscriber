using System.Threading.Tasks;

namespace PB.ITOps.Messaging.PatLite
{
    public interface IHandleEvent<in T>
    {
        Task HandleAsync(T message);
    }
}
