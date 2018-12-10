using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pat.Subscriber.BatchProcessing
{
    public class BatchProcessingBehaviourPipeline
    {
        private readonly ICollection<IBatchProcessingBehaviour> _behaviours;
        private Func<BatchContext, Task> _pipeline;

        public BatchProcessingBehaviourPipeline AddBehaviour(IBatchProcessingBehaviour nextBehaviour)
        {
            _behaviours.Add(nextBehaviour);
            return this;
        }

        public BatchProcessingBehaviourPipeline()
        {
            _behaviours = new List<IBatchProcessingBehaviour>();
        }

        public async Task Invoke(Func<Task> action, CancellationTokenSource tokenSource)
        {
            if (_pipeline == null)
            {
                _pipeline = BuildPipeline();
            }
            await _pipeline(new BatchContext
            {
                Action = action,
                TokenSource = tokenSource
            }).ConfigureAwait(false);
        }

        public void Build()
        {
            BuildPipeline();
        }

        private Func<BatchContext, Task> BuildPipeline()
        {
            Func<BatchContext, Task> current = null;
            foreach (var behaviour in _behaviours.Reverse())
            {

                var next = current;
                current = (ctx) => behaviour.Invoke(next, ctx);
            }
            return current;
        }
    }
}