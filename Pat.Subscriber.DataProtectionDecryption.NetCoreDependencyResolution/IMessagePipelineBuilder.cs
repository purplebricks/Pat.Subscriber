using PB.ITOps.Messaging.PatLite.MessageProcessing;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public interface IMessagePipelineBuilder : IPatLiteOptionsBuilder
    {
        IMessagePipelineBuilder With<T>() where T : IMessageProcessingBehaviour;
    }
}