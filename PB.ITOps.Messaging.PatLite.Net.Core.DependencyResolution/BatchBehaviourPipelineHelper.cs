using System;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.BatchProcessing;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public static class BatchBehaviourPipelineHelper
    {
        public static BatchProcessingBehaviourPipeline AddBehaviour<T>(this BatchProcessingBehaviourPipeline pipleline, IServiceProvider provider) where T : IBatchProcessingBehaviour
        {
            return pipleline.AddBehaviour(provider.GetService<T>());
        }
    }
}