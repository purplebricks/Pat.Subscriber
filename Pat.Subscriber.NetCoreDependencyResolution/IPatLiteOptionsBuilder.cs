using System;
using Pat.Subscriber.CicuitBreaker;
using Pat.Subscriber.Deserialiser;

namespace Pat.Subscriber.NetCoreDependencyResolution
{
    public interface IPatLiteOptionsBuilder
    {
        IMessagePipelineBuilder DefineMessagePipeline { get; }
        IBatchPipelineBuilder DefineBatchPipeline { get; }
        IPatLiteOptionsBuilder WithMessageDeserialiser(Func<IServiceProvider, IMessageDeserialiser> func);

        IPatLiteOptionsBuilder UseDefaultPipelinesWithCircuitBreaker(Func<IServiceProvider, 
            CircuitBreakerBatchProcessingBehaviour.CircuitBreakerOptions> func);
        PatLiteOptions Build();
    }
}