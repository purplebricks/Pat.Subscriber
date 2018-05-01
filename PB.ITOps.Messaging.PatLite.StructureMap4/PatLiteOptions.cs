using System;
using PB.ITOps.Messaging.PatLite.Deserialiser;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public class PatLiteOptions
    {
        public SubscriberConfiguration SubscriberConfiguration { get; set; }
        public BatchPipelineDependencyBuilder BatchMessageProcessingBehaviourDependencyBuilder { get; set; }
        public MessagePipelineDependencyBuilder MessageProcessingPipelineDependencyBuilder { get; set; }
        public Func<IContext, IMessageDeserialiser> MessageDeserialiser { get; set; }
    }
}