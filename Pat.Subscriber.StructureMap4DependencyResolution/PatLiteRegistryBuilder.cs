using System;
using System.Collections.Generic;
using System.Linq;
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
        private string _registerDefaultLoggerWithName;

        public PatLiteRegistryBuilder(SubscriberConfiguration subscriberConfiguration)
        {
            _subscriberConfiguration = subscriberConfiguration;
            _messagePipelineBehaviourTypes.Add(typeof(MonitoringMessageProcessingBehaviour));
            _messagePipelineBehaviourTypes.Add(typeof(DefaultMessageProcessingBehaviour));
            _batchPipelineBehaviourTypes.Add(typeof(MonitoringBatchProcessingBehaviour));
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

        public IPatLiteRegistryBuilder WithDefaultPatLogger(string categoryName = "Pat")
        {
            _registerDefaultLoggerWithName = categoryName;
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

        public PatLiteRegistry Build()
        {
            EnsureTerminatingBehavioursAdded();

            var builder = new MessagePipelineDependencyBuilder(_messagePipelineBehaviourTypes);
            var batchBuilder = new BatchPipelineDependencyBuilder(_batchPipelineBehaviourTypes);
            var patliteOptions = new PatLiteOptions
            {
                SubscriberConfiguration = _subscriberConfiguration,
                MessageProcessingPipelineDependencyBuilder = builder,
                BatchMessageProcessingBehaviourDependencyBuilder = batchBuilder,
                MessageDeserialiser = _messageDeserialiser,
                RegisterDefaultLoggerWithName = _registerDefaultLoggerWithName
            };
            return new PatLiteRegistry(patliteOptions);
        }

        public static implicit operator PatLiteRegistry(PatLiteRegistryBuilder instance)
        {
            return instance.Build();
        }
    }
}