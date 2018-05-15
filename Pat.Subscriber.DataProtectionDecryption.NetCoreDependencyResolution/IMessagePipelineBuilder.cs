using Pat.Subscriber.MessageProcessing;

namespace Pat.Subscriber.DataProtectionDecryption.NetCoreDependencyResolution
{
    public interface IMessagePipelineBuilder : IPatLiteOptionsBuilder
    {
        IMessagePipelineBuilder With<T>() where T : IMessageProcessingBehaviour;
    }
}