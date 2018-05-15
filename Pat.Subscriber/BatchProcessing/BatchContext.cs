using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pat.Subscriber.BatchProcessing
{
    public class BatchContext
    {
        public Func<Task> Action { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
    }
}