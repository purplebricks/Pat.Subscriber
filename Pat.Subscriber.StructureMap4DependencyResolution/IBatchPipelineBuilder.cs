using Pat.Subscriber.BatchProcessing;

namespace Pat.Subscriber.StructureMap4DependencyResolution
{
    public interface IBatchPipelineBuilder: IPatLiteRegistryBuilder
    {
        IBatchPipelineBuilder With<T>() where T : IBatchProcessingBehaviour;
    }
}