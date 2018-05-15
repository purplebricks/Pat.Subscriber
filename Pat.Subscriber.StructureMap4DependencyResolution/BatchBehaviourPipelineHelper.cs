using PB.ITOps.Messaging.PatLite.BatchProcessing;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public static class BatchBehaviourPipelineHelper
    {
        public static BatchProcessingBehaviourPipeline AddBehaviour<T>(this BatchProcessingBehaviourPipeline pipleline, IContext context) where T: IBatchProcessingBehaviour
        {
            return pipleline.AddBehaviour(context.GetInstance<T>());
        }
    }
}