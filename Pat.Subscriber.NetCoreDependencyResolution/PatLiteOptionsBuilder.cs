using System;
using System.Collections.Generic;
using System.Linq;
using Pat.Subscriber.BatchProcessing;
using Pat.Subscriber.CicuitBreaker;
using Pat.Subscriber.Deserialiser;
using Pat.Subscriber.MessageProcessing;

namespace Pat.Subscriber.NetCoreDependencyResolution
{
    public class PatLiteOptionsBuilder : IMessagePipelineBuilder, IBatchPipelineBuilder
    {
        private readonly SubscriberConfiguration _subscriberConfiguration;
        private readonly ICollection<Type> _messagePipelineBehaviourTypes = new List<Type>();
        private readonly ICollection<Type> _batchPipelineBehaviourTypes = new List<Type>();
        private Func<IServiceProvider, IMessageDeserialiser> _messageDeserialiser = provider => new NewtonsoftMessageDeserialiser();
        private Func<IServiceProvider, CircuitBreakerBatchProcessingBehaviour.CircuitBreakerOptions> _circuitBreakerOptions = provider => new CircuitBreakerBatchProcessingBehaviour.CircuitBreakerOptions(1, e => false);

        public PatLiteOptionsBuilder(SubscriberConfiguration subscriberConfiguration)
        {
            _subscriberConfiguration = subscriberConfiguration;
            _messagePipelineBehaviourTypes.Add(typeof(DefaultMessageProcessingBehaviour));
        }

        public IMessagePipelineBuilder With<T>() where T : IMessageProcessingBehaviour
        {
            _messagePipelineBehaviourTypes.Add(typeof(T));
            return this;
        }

        public IMessagePipelineBuilder DefineMessagePipeline
        {
            get
            {
                _messagePipelineBehaviourTypes.Clear();
                return this;
            }
        }

        public IBatchPipelineBuilder DefineBatchPipeline
        {
            get
            {
                _batchPipelineBehaviourTypes.Clear();
                return this;
            }
        }

        public IPatLiteOptionsBuilder WithMessageDeserialiser(Func<IServiceProvider, IMessageDeserialiser> func)
        {
            _messageDeserialiser = func;
            return this;
        }

        IBatchPipelineBuilder IBatchPipelineBuilder.With<T>()
        {
            _batchPipelineBehaviourTypes.Add(typeof(T));
            return this;
        }

        private void EnsureTerminatingBehavioursAdded()
        {
            if (_batchPipelineBehaviourTypes.Count == 0 || _batchPipelineBehaviourTypes.Last() != typeof(DefaultBatchProcessingBehaviour))
            {
                _batchPipelineBehaviourTypes.Add(typeof(DefaultBatchProcessingBehaviour));
            }

            if (_messagePipelineBehaviourTypes.Count == 0 || _messagePipelineBehaviourTypes.Last() != typeof(InvokeHandlerBehaviour))
            {
                _messagePipelineBehaviourTypes.Add(typeof(InvokeHandlerBehaviour));
            }
        }

        public PatLiteOptions Build()
        {
            EnsureTerminatingBehavioursAdded();

            var builder = new MessagePipelineDependencyBuilder(_messagePipelineBehaviourTypes);
            var batchBuilder = new BatchPipelineDependencyBuilder(_batchPipelineBehaviourTypes);
            var patliteOptions = new PatLiteOptions
            {
                SubscriberConfiguration = _subscriberConfiguration,
                MessageProcessingPipelineBuilder = builder,
                BatchMessageProcessingBehaviourPipelineBuilder = batchBuilder,
                MessageDeserialiser = _messageDeserialiser,
                CircuitBreakerOptions = _circuitBreakerOptions
            };
            return patliteOptions;
        }

        public static implicit operator PatLiteOptions(PatLiteOptionsBuilder instance)
        {
            return instance.Build();
        }

        public IPatLiteOptionsBuilder UseDefaultPipelinesWithCircuitBreaker(Func<IServiceProvider, CircuitBreakerBatchProcessingBehaviour.CircuitBreakerOptions> func)
        {
            _messagePipelineBehaviourTypes.Clear();
            _messagePipelineBehaviourTypes.Add(typeof(DefaultMessageProcessingBehaviour));
            _messagePipelineBehaviourTypes.Add(typeof(CircuitBreakerMessageProcessingBehaviour));
            _batchPipelineBehaviourTypes.Clear();
            _batchPipelineBehaviourTypes.Add(typeof(CircuitBreakerBatchProcessingBehaviour));
            _circuitBreakerOptions = func;
            return this;
        }
    }
}