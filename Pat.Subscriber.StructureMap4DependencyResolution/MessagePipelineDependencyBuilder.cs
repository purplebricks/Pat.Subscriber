using System;
using System.Collections.Generic;
using Pat.Subscriber.MessageProcessing;
using StructureMap;

namespace Pat.Subscriber.StructureMap4DependencyResolution
{
    public class MessagePipelineDependencyBuilder
    {
        private readonly ICollection<Type> _messagePipelineBehaviourTypes;

        public MessagePipelineDependencyBuilder(ICollection<Type> messagePipelineBeviourTypes)
        {
            _messagePipelineBehaviourTypes = messagePipelineBeviourTypes;
        }

        public void RegisterTypes(Registry registry)
        {
            foreach (var messagePipelineBehaviourType in _messagePipelineBehaviourTypes)
            {
                registry.AddType(messagePipelineBehaviourType, messagePipelineBehaviourType);
            }
        }

        public MessageProcessingBehaviourPipeline Build(IContext ctx)
        {
            var pipeline = new MessageProcessingBehaviourPipeline();
            foreach (var messagePipelineBehaviourType in _messagePipelineBehaviourTypes)
            {
                pipeline.AddBehaviour((IMessageProcessingBehaviour) ctx.GetInstance(messagePipelineBehaviourType));
            }
            return pipeline;
        }
    }
}