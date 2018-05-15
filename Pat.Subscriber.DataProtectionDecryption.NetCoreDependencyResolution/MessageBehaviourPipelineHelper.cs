using System;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.MessageProcessing;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public static class MessageBehaviourPipelineHelper
    {
        public static MessageProcessingBehaviourPipeline AddBehaviour<T>(this MessageProcessingBehaviourPipeline pipleline, IServiceProvider provider) where T : IMessageProcessingBehaviour
        {
            return pipleline.AddBehaviour(provider.GetService<T>());
        }
    }
}