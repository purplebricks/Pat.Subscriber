using Pat.Subscriber.MessageProcessing;
using StructureMap;

namespace Pat.Subscriber.StructureMap4DependencyResolution
{
    public static class MessageProcessingBehaviourPipelineHelper
    {
        public static MessageProcessingBehaviourPipeline AddBehaviour<T>(this MessageProcessingBehaviourPipeline pipleline, IContext context) where T : IMessageProcessingBehaviour
        {
            return pipleline.AddBehaviour(context.GetInstance<T>());
        }
    }
}