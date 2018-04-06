using System;
using PB.ITOps.Messaging.PatLite.Deserialiser;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public interface IPatLiteOptionsBuilder
    {
        IMessagePipelineBuilder DefineMessagePipeline { get; }
        IBatchPipelineBuilder DefineBatchPipeline { get; }
        IPatLiteOptionsBuilder WithMessageDeserialiser(Func<IServiceProvider, IMessageDeserialiser> func);
        PatLiteOptions Build();
    }
}