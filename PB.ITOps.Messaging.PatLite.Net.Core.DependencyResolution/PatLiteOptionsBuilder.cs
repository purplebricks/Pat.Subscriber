using System;
using System.Collections.Generic;
using PB.ITOps.Messaging.PatLite.BatchProcessing;
using PB.ITOps.Messaging.PatLite.CicuitBreaker;
using PB.ITOps.Messaging.PatLite.Deserialiser;
using PB.ITOps.Messaging.PatLite.MessageProcessing;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public class PatLiteOptionsBuilder : IMessagePipelineBuilder, IBatchPipelineBuilder
    {
        private readonly SubscriberConfiguration _subscriberConfiguration;
        private readonly ICollection<Type> _messagePipelineBehaviourTypes = new List<Type>();
        private readonly ICollection<Type> _batchPipelineBehaviourTypes = new List<Type>();
        private Func<IServiceProvider, IMessageDeserialiser> _messageDeserialiser = provider => new NewtonsoftMessageDeserialiser();
        private CircuitBreakerBatchProcessingBehaviour.CircuitBreakerOptions _circuitBreakerOptions;

        public PatLiteOptionsBuilder(SubscriberConfiguration subscriberConfiguration)
        {
            _subscriberConfiguration = subscriberConfiguration;
            _messagePipelineBehaviourTypes.Add(typeof(DefaultMessageProcessingBehaviour));
            _messagePipelineBehaviourTypes.Add(typeof(MonitoringPolicy.MonitoringMessageProcessingBehaviour));
            _messagePipelineBehaviourTypes.Add(typeof(InvokeHandlerBehaviour));
            _batchPipelineBehaviourTypes.Add(typeof(MonitoringPolicy.MonitoringBatchProcessingBehaviour));
            _batchPipelineBehaviourTypes.Add(typeof(DefaultBatchProcessingBehaviour));
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

        public PatLiteOptions Build()
        {
            var builder = new MessagePipelineDependencyBuilder(_messagePipelineBehaviourTypes);
            var batchBuilder = new BatchPipelineDependencyBuilder(_batchPipelineBehaviourTypes);
            var patliteOptions = new PatLiteOptions
            {
                SubscriberConfiguration = _subscriberConfiguration,
                MessageProcessingPipelineBuilder = builder,
                BatchMessageProcessingBehaviourPipelineBuilder = batchBuilder,
                MessageDeserialiser = _messageDeserialiser
            };
            return patliteOptions;
        }

        public static implicit operator PatLiteOptions(PatLiteOptionsBuilder instance)
        {
            return instance.Build();
        }

        public IPatLiteOptionsBuilder UseDefaultPipelinesWithCircuitBreaker(CircuitBreakerBatchProcessingBehaviour.CircuitBreakerOptions circuitBreakerOptions)
        {
            _messagePipelineBehaviourTypes.Clear();
            _messagePipelineBehaviourTypes.Add(typeof(DefaultMessageProcessingBehaviour));
            _messagePipelineBehaviourTypes.Add(typeof(CircuitBreakerMessageProcessingBehaviour));
            _messagePipelineBehaviourTypes.Add(typeof(MonitoringPolicy.MonitoringMessageProcessingBehaviour));
            _messagePipelineBehaviourTypes.Add(typeof(InvokeHandlerBehaviour));
            _batchPipelineBehaviourTypes.Clear();
            _batchPipelineBehaviourTypes.Add(typeof(CircuitBreakerBatchProcessingBehaviour));
            _batchPipelineBehaviourTypes.Add(typeof(MonitoringPolicy.MonitoringBatchProcessingBehaviour));
            _batchPipelineBehaviourTypes.Add(typeof(DefaultBatchProcessingBehaviour));
            _circuitBreakerOptions = circuitBreakerOptions;
            return this;
        }
    }
}