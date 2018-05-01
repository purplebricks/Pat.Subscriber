using System;
using PB.ITOps.Messaging.PatLite.Deserialiser;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public interface IPatLiteRegistryBuilder
    {
        IMessagePipelineBuilder DefineMessagePipeline { get; }
        IBatchPipelineBuilder DefineBatchPipeline { get; }
        IPatLiteRegistryBuilder WithMessageDeserialiser(Func<IContext, IMessageDeserialiser> func);
        PatLiteRegistry Build();
    }
}