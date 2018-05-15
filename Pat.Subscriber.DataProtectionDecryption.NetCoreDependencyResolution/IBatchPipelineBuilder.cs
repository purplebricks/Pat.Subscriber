using Pat.Subscriber.BatchProcessing;

namespace Pat.Subscriber.DataProtectionDecryption.NetCoreDependencyResolution
{
    public interface IBatchPipelineBuilder : IPatLiteOptionsBuilder
    {
        IBatchPipelineBuilder With<T>() where T : IBatchProcessingBehaviour;
    }
}