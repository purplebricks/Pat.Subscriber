using System;
using Microsoft.Extensions.DependencyInjection;
using Pat.Subscriber.BatchProcessing;

namespace Pat.Subscriber.DataProtectionDecryption.NetCoreDependencyResolution
{
    public static class BatchBehaviourPipelineHelper
    {
        public static BatchProcessingBehaviourPipeline AddBehaviour<T>(this BatchProcessingBehaviourPipeline pipleline, IServiceProvider provider) where T : IBatchProcessingBehaviour
        {
            return pipleline.AddBehaviour(provider.GetService<T>());
        }
    }
}