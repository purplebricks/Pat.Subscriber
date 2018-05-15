using System;
using System.Collections.Generic;
using PB.ITOps.Messaging.PatLite.MessageProcessing;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
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