using PB.ITOps.Messaging.PatLite.MessageProcessing;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public interface IMessagePipelineBuilder: IPatLiteRegistryBuilder
    {
        IMessagePipelineBuilder With<T>() where T : IMessageProcessingBehaviour;
    }
}