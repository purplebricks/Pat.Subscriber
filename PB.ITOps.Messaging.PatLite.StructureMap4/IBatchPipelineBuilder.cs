using PB.ITOps.Messaging.PatLite.BatchProcessing;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public interface IBatchPipelineBuilder: IPatLiteRegistryBuilder
    {
        IBatchPipelineBuilder With<T>() where T : IBatchProcessingBehaviour;
    }
}