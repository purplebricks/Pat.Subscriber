using System;
using System.Threading.Tasks;

namespace PB.ITOps.Messaging.PatLite.BatchProcessing
{
    public interface IBatchProcessingBehaviour
    {
        Task<int> Invoke(Func<BatchContext, Task<int>> next, BatchContext context);
    }
}