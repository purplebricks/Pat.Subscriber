using PB.ITOps.Messaging.PatLite.BatchProcessing;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public interface IBatchPipelineBuilder : IPatLiteOptionsBuilder
    {
        IBatchPipelineBuilder With<T>() where T : IBatchProcessingBehaviour;
    }
}