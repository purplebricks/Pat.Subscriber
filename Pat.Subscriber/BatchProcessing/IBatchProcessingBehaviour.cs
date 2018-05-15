using System;
using System.Threading.Tasks;

namespace Pat.Subscriber.BatchProcessing
{
    public interface IBatchProcessingBehaviour
    {
        Task Invoke(Func<BatchContext, Task> next, BatchContext context);
    }
}