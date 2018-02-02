using PB.ITOps.Messaging.PatLite.MessageProcessing;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public static class MessageProcessingBehaviourPipelineHelper
    {
        public static MessageProcessingBehaviourPipeline AddBehaviour<T>(this MessageProcessingBehaviourPipeline pipleline, IContext context) where T : IMessageProcessingBehaviour
        {
            return pipleline.AddBehaviour(context.GetInstance<T>());
        }
    }
}