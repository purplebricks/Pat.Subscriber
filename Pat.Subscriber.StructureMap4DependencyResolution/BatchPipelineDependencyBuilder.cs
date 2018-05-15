using System;
using System.Collections.Generic;
using Pat.Subscriber.BatchProcessing;
using StructureMap;

namespace Pat.Subscriber.StructureMap4DependencyResolution
{
    public class BatchPipelineDependencyBuilder
    {
        private readonly ICollection<Type> _batchPipelineBehaviourTypes;

        public BatchPipelineDependencyBuilder(ICollection<Type> batchPipelineBeviourTypes)
        {
            _batchPipelineBehaviourTypes = batchPipelineBeviourTypes;
        }

        public void RegisterTypes(Registry registry)
        {
            foreach (var batchPipelineBehaviourType in _batchPipelineBehaviourTypes)
            {
                registry.AddType(batchPipelineBehaviourType, batchPipelineBehaviourType);
            }
        }

        public BatchProcessingBehaviourPipeline Build(IContext ctx)
        {
            var pipeline = new BatchProcessingBehaviourPipeline();
            foreach (var batchPipelineBehaviourType in _batchPipelineBehaviourTypes)
            {
                pipeline.AddBehaviour((IBatchProcessingBehaviour)ctx.GetInstance(batchPipelineBehaviourType));
            }
            return pipeline;
        }
    }
}