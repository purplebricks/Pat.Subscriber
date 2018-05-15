using Pat.Subscriber.MessageProcessing;

namespace Pat.Subscriber.StructureMap4DependencyResolution
{
    public interface IMessagePipelineBuilder: IPatLiteRegistryBuilder
    {
        IMessagePipelineBuilder With<T>() where T : IMessageProcessingBehaviour;
    }
}