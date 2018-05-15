using System;
using Microsoft.Extensions.DependencyInjection;
using Pat.Subscriber.MessageProcessing;

namespace Pat.Subscriber.DataProtectionDecryption.NetCoreDependencyResolution
{
    public static class MessageBehaviourPipelineHelper
    {
        public static MessageProcessingBehaviourPipeline AddBehaviour<T>(this MessageProcessingBehaviourPipeline pipleline, IServiceProvider provider) where T : IMessageProcessingBehaviour
        {
            return pipleline.AddBehaviour(provider.GetService<T>());
        }
    }
}