using System;
using Pat.Subscriber.Deserialiser;
using StructureMap;

namespace Pat.Subscriber.StructureMap4DependencyResolution
{
    public interface IPatLiteRegistryBuilder
    {
        IMessagePipelineBuilder DefineMessagePipeline { get; }
        IBatchPipelineBuilder DefineBatchPipeline { get; }
        IPatLiteRegistryBuilder WithMessageDeserialiser(Func<IContext, IMessageDeserialiser> func);
        PatLiteRegistry Build();
    }
}