using System;
using System.Threading.Tasks;

namespace PB.ITOps.Messaging.PatLite.BatchProcessing
{
    public interface IBatchProcessingBehaviour
    {
        Task Invoke(Func<BatchContext, Task> next, BatchContext context);
    }
}