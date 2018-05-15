using System;
using System.Collections.Generic;
using Pat.Subscriber.BatchProcessing;
using Pat.Subscriber.Deserialiser;
using Pat.Subscriber.MessageProcessing;
using Pat.Subscriber.Telemetry.StatsD;
using StructureMap;

namespace Pat.Subscriber.StructureMap4DependencyResolution
{
    public class PatLiteRegistryBuilder: IMessagePipelineBuilder, IBatchPipelineBuilder
    {
        private readonly ICollection<Type> _messagePipelineBehaviourTypes = new List<Type>();
        private readonly ICollection<Type> _batchPipelineBehaviourTypes = new List<Type>();
        private readonly SubscriberConfiguration _subscriberConfiguration;
        private Func<IContext, IMessageDeserialiser> _messageDeserialiser = provider => new NewtonsoftMessageDeserialiser();

        public PatLiteRegistryBuilder(SubscriberConfiguration subscriberConfiguration)
        {
            _subscriberConfiguration = subscriberConfiguration;
            _messagePipelineBehaviourTypes.Add(typeof(MonitoringMessageProcessingBehaviour));
            _messagePipelineBehaviourTypes.Add(typeof(DefaultMessageProcessingBehaviour));
            _messagePipelineBehaviourTypes.Add(typeof(InvokeHandlerBehaviour));
            _batchPipelineBehaviourTypes.Add(typeof(MonitoringBatchProcessingBehaviour));
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

        IBatchPipelineBuilder IBatchPipelineBuilder.With<T>()
        {
            _batchPipelineBehaviourTypes.Add(typeof(T));
            return this;
        }

        public IPatLiteRegistryBuilder WithMessageDeserialiser(Func<IContext, IMessageDeserialiser> func)
        {
            _messageDeserialiser = func;
            return this;
        }

        public PatLiteRegistry Build()
        {
            var builder = new MessagePipelineDependencyBuilder(_messagePipelineBehaviourTypes);
            var batchBuilder = new BatchPipelineDependencyBuilder(_batchPipelineBehaviourTypes);
            var patliteOptions = new PatLiteOptions
            {
                SubscriberConfiguration = _subscriberConfiguration,
                MessageProcessingPipelineDependencyBuilder = builder,
                BatchMessageProcessingBehaviourDependencyBuilder = batchBuilder,
                MessageDeserialiser = _messageDeserialiser
            };
            return new PatLiteRegistry(patliteOptions);
        }

        public static implicit operator PatLiteRegistry(PatLiteRegistryBuilder instance)
        {
            return instance.Build();
        }
    }
}