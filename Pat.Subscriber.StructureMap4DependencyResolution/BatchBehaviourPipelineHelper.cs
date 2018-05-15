using Pat.Subscriber.BatchProcessing;
using StructureMap;

namespace Pat.Subscriber.StructureMap4DependencyResolution
{
    public static class BatchBehaviourPipelineHelper
    {
        public static BatchProcessingBehaviourPipeline AddBehaviour<T>(this BatchProcessingBehaviourPipeline pipleline, IContext context) where T: IBatchProcessingBehaviour
        {
            return pipleline.AddBehaviour(context.GetInstance<T>());
        }
    }
}