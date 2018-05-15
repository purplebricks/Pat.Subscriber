using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.MessageProcessing;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public class MessagePipelineDependencyBuilder
    {
        private readonly ICollection<Type> _messagePipelineBehaviourTypes;

        public MessagePipelineDependencyBuilder(ICollection<Type> messagePipelineBeviourTypes)
        {
            _messagePipelineBehaviourTypes = messagePipelineBeviourTypes;
        }

        public void RegisterTypes(IServiceCollection serviceCollection)
        {
            foreach (var messagePipelineBehaviourType in _messagePipelineBehaviourTypes)
            {
                serviceCollection.AddSingleton(messagePipelineBehaviourType);
            }
        }

        public MessageProcessingBehaviourPipeline Build(IServiceProvider provider)
        {
            var pipeline = new MessageProcessingBehaviourPipeline();
            foreach (var messagePipelineBehaviourType in _messagePipelineBehaviourTypes)
            {
                pipeline.AddBehaviour((IMessageProcessingBehaviour)provider.GetService(messagePipelineBehaviourType));
            }
            return pipeline;
        }
    }
}