using System;
using Pat.Subscriber.CicuitBreaker;
using Pat.Subscriber.Deserialiser;

namespace Pat.Subscriber.DataProtectionDecryption.NetCoreDependencyResolution
{
    public interface IPatLiteOptionsBuilder
    {
        IMessagePipelineBuilder DefineMessagePipeline { get; }
        IBatchPipelineBuilder DefineBatchPipeline { get; }
        IPatLiteOptionsBuilder WithMessageDeserialiser(Func<IServiceProvider, IMessageDeserialiser> func);

        IPatLiteOptionsBuilder UseDefaultPipelinesWithCircuitBreaker(
            CircuitBreakerBatchProcessingBehaviour.CircuitBreakerOptions circuitBreakerOptions);
        PatLiteOptions Build();
    }
}