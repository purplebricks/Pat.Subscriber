using Pat.Subscriber.BatchProcessing;

namespace Pat.Subscriber.NetCoreDependencyResolution
{
    public interface IBatchPipelineBuilder : IPatLiteOptionsBuilder
    {
        IBatchPipelineBuilder With<T>() where T : IBatchProcessingBehaviour;
    }
}