using System;
using Pat.Subscriber.Deserialiser;
using StructureMap;

namespace Pat.Subscriber.StructureMap4DependencyResolution
{
    public class PatLiteOptions
    {
        public SubscriberConfiguration SubscriberConfiguration { get; set; }
        public BatchPipelineDependencyBuilder BatchMessageProcessingBehaviourDependencyBuilder { get; set; }
        public MessagePipelineDependencyBuilder MessageProcessingPipelineDependencyBuilder { get; set; }
        public Func<IContext, IMessageDeserialiser> MessageDeserialiser { get; set; }
    }
}