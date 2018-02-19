using System;
using System.Collections.Generic;
using PB.ITOps.Messaging.PatLite.BatchProcessing;
using PB.ITOps.Messaging.PatLite.MessageProcessing;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public class PatLiteRegistryBuilder: IMessagePipelineBuilder, IBatchPipelineBuilder
    {
        private readonly ICollection<Type> _messagePipelineBehaviourTypes = new List<Type>();
        private readonly ICollection<Type> _batchPipelineBehaviourTypes = new List<Type>();
        private SubscriberConfiguration _subscriberConfiguration = new SubscriberConfiguration();

        public PatLiteRegistryBuilder()
        {
            _messagePipelineBehaviourTypes.Add(typeof(DefaultMessageProcessingBehaviour));
            _messagePipelineBehaviourTypes.Add(typeof(InvokeHandlerBehaviour));
            _batchPipelineBehaviourTypes.Add(typeof(DefaultBatchProcessingBehaviour));
        }

        public IMessagePipelineBuilder With<T>() where T : IMessageProcessingBehaviour
        {
            _messagePipelineBehaviourTypes.Add(typeof(T));
            return this;
        }

        public IMessagePipelineBuilder DefineMessagePipeline()
        {
            _messagePipelineBehaviourTypes.Clear();
            return this;
        }

        public IBatchPipelineBuilder DefineBatchPipeline()
        {
            _batchPipelineBehaviourTypes.Clear();
            return this;
        }

        public IPatLiteRegistryBuilder Use(SubscriberConfiguration subscriberConfiguration)
        {
            _subscriberConfiguration = subscriberConfiguration;
            return this;
        }

        IBatchPipelineBuilder IBatchPipelineBuilder.With<T>()
        {
            _batchPipelineBehaviourTypes.Add(typeof(T));
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
                BatchMessageProcessingBehaviourDependencyBuilder = batchBuilder
            };
            return new PatLiteRegistry(patliteOptions);
        }

        public static implicit operator PatLiteRegistry(PatLiteRegistryBuilder instance)
        {
            return instance.Build();
        }
    }
}