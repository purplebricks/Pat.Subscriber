using PB.ITOps.Messaging.PatLite.BatchProcessing;
using PB.ITOps.Messaging.PatLite.MessageProcessing;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public class PatLiteOptions
    {
        public SubscriberConfiguration SubscriberConfiguration { get; set; }
        public BatchPipelineDependencyBuilder BatchMessageProcessingBehaviourDependencyBuilder { get; set; }
        public MessagePipelineDependencyBuilder MessageProcessingPipelineDependencyBuilder { get; set; }
    }
}