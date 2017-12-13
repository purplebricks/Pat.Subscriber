using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PB.ITOps.Messaging.PatLite.BatchProcessing
{
    public class BatchProcessingBehaviourPipeline
    {
        private readonly ICollection<IBatchProcessingBehaviour> _behaviours;
        private Func<BatchContext, Task<int>> _pipeline;

        public BatchProcessingBehaviourPipeline AddBehaviour(IBatchProcessingBehaviour nextBehaviour)
        {
            _behaviours.Add(nextBehaviour);
            return this;
        }

        public BatchProcessingBehaviourPipeline()
        {
            _behaviours = new List<IBatchProcessingBehaviour>();
        }

        public async Task Invoke(Func<Task<int>> action, CancellationTokenSource tokenSource)
        {
            if (_pipeline == null)
            {
                _pipeline = BuildPipeline();
            }
            await _pipeline(new BatchContext
            {
                Action = action,
                TokenSource = tokenSource
            });
        }

        public void Build()
        {
            BuildPipeline();
        }

        private Func<BatchContext, Task<int>> BuildPipeline()
        {
            Func<BatchContext, Task<int>> current = null;
            foreach (var behaviour in _behaviours.Reverse())
            {

                var next = current;
                current = (ctx) => behaviour.Invoke(next, ctx);
            }
            return current;
        }
    }
}