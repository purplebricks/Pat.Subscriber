using System;
using System.Threading;
using System.Threading.Tasks;

namespace PB.ITOps.Messaging.PatLite.BatchProcessing
{
    public class BatchContext
    {
        public Func<Task<int>> Action { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
    }
}